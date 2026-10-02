using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Views;

/// <summary>
/// A saved view filter is a flat list of rows that form a tree through <c>parentId</c>: one root
/// group, whose <c>groupOperator</c> joins its children, with conditions and nested groups under it.
/// Only the root is ever evaluated, so conditions listed side by side (every <c>parentId</c> null)
/// used to save without error and then filter on the first row alone.
/// </summary>
public static class FilterConditionTree
{
    private const string HowToShape =
        "A filter must be one group (type filter_condition_group, parentId null) that holds every "
        + "condition: give each condition a parentId equal to that group's id, and set the group's "
        + "groupOperator to and or or.";

    /// <summary>What is wrong with the shape of <paramref name="filter"/>; empty when it is valid or empty.</summary>
    public static IReadOnlyList<string> Problems(IReadOnlyList<FilterConditionInputDto> filter)
    {
        if (filter.Count == 0)
            return [];

        var problems = new List<string>();
        if (filter.Any(row => row.Id == Guid.Empty) || filter.Select(row => row.Id).Distinct().Count() != filter.Count)
            return ["Every filter row needs its own id (a GUID). " + HowToShape];

        var roots = filter.Where(row => row.ParentId is null).ToList();
        if (filter.Count == 1 && roots.Count == 1 && !IsGroup(roots[0]))
            return []; // A lone condition is unambiguous.

        if (roots.Count != 1 || !IsGroup(roots[0]))
            return [HowToShape + (roots.Count > 1 ? " Only the first of the rows with no parentId would be applied." : "")];

        var groups = filter.Where(IsGroup).Select(row => row.Id).ToHashSet();
        var children = filter.Where(row => row.ParentId is not null).ToLookup(row => row.ParentId!.Value);

        foreach (var row in filter.Where(row => row.ParentId is { } parent && !groups.Contains(parent)))
            problems.Add($"Filter row {Describe(row)} has a parentId that is not a group in this filter.");

        var reached = new HashSet<Guid>();
        var pending = new Stack<Guid>([roots[0].Id]);
        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!reached.Add(id))
                continue;
            foreach (var child in children[id])
                pending.Push(child.Id);
        }
        if (problems.Count == 0 && reached.Count != filter.Count)
            problems.Add("Some filter rows are not connected to the root group through parentId.");

        foreach (var group in filter.Where(IsGroup))
        {
            var rows = children[group.Id].Count();
            if (rows > 1 && !HasJoiner(group))
                problems.Add("A filter group with more than one row needs a groupOperator of and or or.");
        }
        return problems;
    }

    private static bool IsGroup(FilterConditionInputDto row) =>
        string.Equals(row.Type, FilterConditionType.FilterConditionGroup.DeveloperName, StringComparison.OrdinalIgnoreCase);

    private static bool HasJoiner(FilterConditionInputDto group) =>
        string.Equals(group.GroupOperator, BooleanOperator.AND.DeveloperName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(group.GroupOperator, BooleanOperator.OR.DeveloperName, StringComparison.OrdinalIgnoreCase);

    private static string Describe(FilterConditionInputDto row) =>
        string.IsNullOrEmpty(row.Field) ? row.Id.ToString() : $"on '{row.Field}'";
}
