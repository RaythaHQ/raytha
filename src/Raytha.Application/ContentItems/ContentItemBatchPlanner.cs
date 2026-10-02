using System.Text.Json;
using CSharpVitamins;

namespace Raytha.Application.ContentItems;

/// <summary>
/// Pure planning for a batch create: turns the relationship values in each item (an id, a route
/// path, or a primary field value) into ids that are known now or items of the same batch that
/// must be created first, and orders the batch so dependencies come before the items that need
/// them. It reads no data; callers hand in the resolutions they looked up.
/// </summary>
public static class ContentItemBatchPlanner
{
    /// <summary>The most items one batch request may carry.</summary>
    public const int MaxBatchSize = 500;

    public sealed record RelationshipField(
        string DeveloperName,
        string Label,
        Guid RelatedContentTypeId
    );

    /// <summary>
    /// The outcome of looking one reference up among existing items: the matched id when there is
    /// exactly one, and how many items matched (capped at two, which already means ambiguous).
    /// </summary>
    public sealed record ReferenceMatch(Guid? Id, int Matches);

    public sealed record ItemError(string? Field, string Message);

    public sealed class ItemPlan
    {
        public required int Index { get; init; }

        /// <summary>Relationship field to the id of an existing item, ready to store.</summary>
        public Dictionary<string, string> Resolved { get; } = new();

        /// <summary>Relationship field to the index of the batch item that must be created first.</summary>
        public Dictionary<string, int> DependsOn { get; } = new();

        public List<ItemError> Errors { get; } = [];
    }

    public sealed record BatchPlan(IReadOnlyList<ItemPlan> Items, IReadOnlyList<int> Order);

    public sealed record Reference(int ItemIndex, RelationshipField Field, string Value);

    /// <summary>
    /// The text of a field value that can name an item. Anything other than a non-blank string is
    /// treated as no reference and left for the create command to validate.
    /// </summary>
    public static string? ReferenceText(object? raw)
    {
        var text = raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    public static IReadOnlyList<Reference> CollectReferences(
        IReadOnlyList<IDictionary<string, dynamic>> contents,
        IReadOnlyList<RelationshipField> fields
    )
    {
        var references = new List<Reference>();
        for (var index = 0; index < contents.Count; index++)
        {
            foreach (var field in fields)
            {
                if (!contents[index].TryGetValue(field.DeveloperName, out object? raw))
                {
                    continue;
                }

                var text = ReferenceText(raw);
                if (text is not null)
                {
                    references.Add(new Reference(index, field, text));
                }
            }
        }
        return references;
    }

    public static BatchPlan Plan(
        Guid contentTypeId,
        string primaryFieldDeveloperName,
        string primaryFieldLabel,
        IReadOnlyList<IDictionary<string, dynamic>> contents,
        IReadOnlyList<RelationshipField> fields,
        IReadOnlyDictionary<(Guid ContentTypeId, string Value), ReferenceMatch> lookups
    )
    {
        var plans = Enumerable
            .Range(0, contents.Count)
            .Select(i => new ItemPlan { Index = i })
            .ToArray();

        var primaryValues = contents
            .Select(c =>
                c.TryGetValue(primaryFieldDeveloperName, out object? raw) ? ReferenceText(raw) : null
            )
            .ToArray();

        foreach (var reference in CollectReferences(contents, fields))
        {
            var plan = plans[reference.ItemIndex];
            var field = reference.Field;
            var value = reference.Value;

            var match = lookups.TryGetValue((field.RelatedContentTypeId, value), out var found)
                ? found
                : new ReferenceMatch(null, 0);

            if (match.Id is { } existingId)
            {
                plan.Resolved[field.DeveloperName] = ((ShortGuid)existingId).ToString();
                continue;
            }

            if (match.Matches > 1)
            {
                plan.Errors.Add(
                    new ItemError(
                        field.DeveloperName,
                        $"'{value}' matches more than one existing item. Use the item's id or route path."
                    )
                );
                continue;
            }

            if (field.RelatedContentTypeId == contentTypeId)
            {
                var candidates = Enumerable
                    .Range(0, contents.Count)
                    .Where(j =>
                        j != reference.ItemIndex
                        && string.Equals(primaryValues[j], value, StringComparison.Ordinal)
                    )
                    .ToArray();

                if (candidates.Length == 1)
                {
                    plan.DependsOn[field.DeveloperName] = candidates[0];
                    continue;
                }

                if (candidates.Length > 1)
                {
                    plan.Errors.Add(
                        new ItemError(
                            field.DeveloperName,
                            $"'{value}' matches {candidates.Length} items in this batch. Use the item's id or route path."
                        )
                    );
                    continue;
                }
            }

            plan.Errors.Add(
                new ItemError(
                    field.DeveloperName,
                    $"No item found for '{field.Label}' with id, route path, or {primaryFieldLabel} '{value}'."
                )
            );
        }

        return new BatchPlan(plans, Order(plans));
    }

    /// <summary>
    /// Orders items so each comes after the batch items it depends on. Items caught in a cycle
    /// are left out of the order and given an error. Ties keep the order the caller sent.
    /// </summary>
    private static List<int> Order(ItemPlan[] plans)
    {
        var order = new List<int>(plans.Length);
        var placed = new bool[plans.Length];

        var progressed = true;
        while (progressed)
        {
            progressed = false;
            foreach (var plan in plans)
            {
                if (placed[plan.Index] || plan.DependsOn.Values.Any(j => !placed[j]))
                {
                    continue;
                }

                placed[plan.Index] = true;
                order.Add(plan.Index);
                progressed = true;
            }
        }

        foreach (var plan in plans.Where(p => !placed[p.Index]))
        {
            // Not placed: waiting on a cycle, or on an item that is.
            var stuck = string.Join(
                ", ",
                plan.DependsOn.Values.Where(j => !placed[j]).Distinct().Order()
            );
            plan.Errors.Add(
                new ItemError(
                    plan.DependsOn.Keys.First(),
                    $"Circular reference: this item cannot be created because item(s) {stuck} it depends on cannot be created first."
                )
            );
        }

        return order;
    }

    /// <summary>
    /// The content to create an item with: the original values, with each reference replaced by
    /// the id it resolved to. A reference to a batch item that was not created is an error.
    /// </summary>
    public static Dictionary<string, dynamic> BuildContent(
        ItemPlan plan,
        IDictionary<string, dynamic> content,
        IReadOnlyDictionary<int, ShortGuid> created,
        List<ItemError> errors
    )
    {
        var built = new Dictionary<string, dynamic>(content);

        foreach (var (field, id) in plan.Resolved)
        {
            built[field] = id;
        }

        foreach (var (field, dependency) in plan.DependsOn)
        {
            if (created.TryGetValue(dependency, out var dependencyId))
            {
                built[field] = dependencyId.ToString();
            }
            else
            {
                errors.Add(
                    new ItemError(
                        field,
                        $"This item depends on item {dependency}, which was not created."
                    )
                );
            }
        }

        return built;
    }
}
