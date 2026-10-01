using System.Text.Json;
using CSharpVitamins;
using Raytha.Application.Common.Utils;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes;

/// <summary>
/// Works out what importing a <see cref="SchemaDocument"/> would do to the site, without touching
/// it: which content types, fields, and views would be created or changed, and every reason the
/// document cannot be applied. Import is an upsert by developer name. It never deletes, so
/// anything the document leaves out stays as it is.
/// </summary>
public static class SchemaImportPlanner
{
    public enum PlanAction
    {
        Unchanged,
        Created,
        Updated,
    }

    public sealed record TemplateInfo(
        Guid Id,
        string DeveloperName,
        bool IsBaseLayout,
        bool AllowAccessForNewContentTypes,
        IReadOnlySet<Guid> AccessibleContentTypeIds
    );

    /// <summary>The current site, as loaded by <see cref="SchemaImportSnapshot"/>.</summary>
    public sealed record ExistingSchema(
        IReadOnlyList<ContentType> ContentTypes,
        IReadOnlyDictionary<Guid, IReadOnlySet<string>> DeletedFieldNames,
        IReadOnlySet<string> RoutePaths,
        IReadOnlyDictionary<Guid, WebTemplateViewRelation> ViewRelations,
        IReadOnlyList<TemplateInfo> Templates
    );

    public sealed class TypePlan
    {
        public required string DeveloperName { get; init; }
        public required SchemaContentType Document { get; init; }
        public required Guid Id { get; init; }
        public ContentType? Existing { get; init; }
        public PlanAction Action { get; set; }
        public List<string> Changes { get; } = [];
        public List<FieldPlan> Fields { get; } = [];
        public List<ViewPlan> Views { get; } = [];
        public Guid PrimaryFieldId { get; set; }

        /// <summary>Existing fields the document does not list, to the order they move to.</summary>
        public Dictionary<Guid, int> UnlistedOrders { get; } = [];
    }

    public sealed class FieldPlan
    {
        public required string DeveloperName { get; init; }
        public required SchemaField Document { get; init; }
        public required Guid Id { get; init; }
        public ContentTypeField? Existing { get; init; }
        public required BaseFieldType FieldType { get; init; }
        public Guid? RelatedContentTypeId { get; init; }
        public int Order { get; set; }
        public PlanAction Action { get; set; }
        public List<string> Changes { get; } = [];
    }

    public sealed class ViewPlan
    {
        public required string DeveloperName { get; init; }
        public required SchemaView Document { get; init; }
        public required Guid Id { get; init; }
        public View? Existing { get; init; }
        public PlanAction Action { get; set; }
        public List<string> Changes { get; } = [];

        /// <summary>The route path to set, or null to leave an existing view's path alone.</summary>
        public string? RoutePath { get; set; }

        /// <summary>The list template to use, or null to leave an existing view's template alone.</summary>
        public Guid? TemplateId { get; set; }
        public List<ColumnSortOrder> Sort { get; } = [];
        public List<FilterCondition> Filter { get; } = [];
    }

    public sealed record ImportPlan(
        IReadOnlyList<TypePlan> Types,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings
    );

    public static ImportPlan Plan(SchemaDocument document, ExistingSchema existing)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (document.SchemaVersion != SchemaDocument.CurrentVersion)
        {
            errors.Add(
                $"schemaVersion {document.SchemaVersion} is not supported; this server reads version {SchemaDocument.CurrentVersion}."
            );
        }

        var documentTypes = document.ContentTypes ?? [];
        if (documentTypes.Count == 0)
        {
            errors.Add("The schema has no content types.");
        }

        var existingByName = existing.ContentTypes.ToDictionary(c => c.DeveloperName!);
        var typeIds = existing.ContentTypes.ToDictionary(c => c.DeveloperName!, c => c.Id);
        var usedPaths = new HashSet<string>(existing.RoutePaths, StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>();
        var validTypes = new List<(SchemaContentType Document, string Name)>();
        foreach (var documentType in documentTypes)
        {
            var name = Name(documentType.DeveloperName);
            if (!StringExtensions.IsValidDeveloperName(documentType.DeveloperName ?? ""))
            {
                errors.Add($"Content type '{documentType.DeveloperName}' has an invalid developer name.");
                continue;
            }
            if (!seen.Add(name))
            {
                errors.Add($"Content type '{name}' appears more than once.");
                continue;
            }
            typeIds.TryAdd(name, Guid.NewGuid());
            validTypes.Add((documentType, name));
        }

        var plans = new List<TypePlan>();
        foreach (var (documentType, name) in validTypes)
        {
            existingByName.TryGetValue(name, out var existingType);
            var plan = PlanType(
                documentType,
                name,
                existingType,
                typeIds,
                existing,
                usedPaths,
                errors,
                warnings
            );
            plans.Add(plan);
        }

        return new ImportPlan(plans, errors, warnings);
    }

    private static TypePlan PlanType(
        SchemaContentType documentType,
        string name,
        ContentType? existingType,
        Dictionary<string, Guid> typeIds,
        ExistingSchema existing,
        HashSet<string> usedPaths,
        List<string> errors,
        List<string> warnings
    )
    {
        var plan = new TypePlan
        {
            DeveloperName = name,
            Document = documentType,
            Id = typeIds[name],
            Existing = existingType,
            Action = existingType is null ? PlanAction.Created : PlanAction.Unchanged,
        };

        if (string.IsNullOrWhiteSpace(documentType.LabelPlural))
            errors.Add($"{name}: labelPlural is required.");
        if (string.IsNullOrWhiteSpace(documentType.LabelSingular))
            errors.Add($"{name}: labelSingular is required.");
        if (string.IsNullOrWhiteSpace(documentType.DefaultRouteTemplate))
            errors.Add($"{name}: defaultRouteTemplate is required.");
        else if (documentType.DefaultRouteTemplate.IsProtectedRoutePath())
            errors.Add($"{name}: defaultRouteTemplate cannot begin with a protected path.");

        if (existingType is not null)
        {
            Track(plan.Changes, "labelPlural", existingType.LabelPlural, documentType.LabelPlural);
            Track(plan.Changes, "labelSingular", existingType.LabelSingular, documentType.LabelSingular);
            Track(plan.Changes, "description", Text(existingType.Description), Text(documentType.Description));
            Track(
                plan.Changes,
                "defaultRouteTemplate",
                existingType.DefaultRouteTemplate,
                documentType.DefaultRouteTemplate
            );
        }

        var fieldNames = PlanFields(plan, documentType, name, existingType, typeIds, existing, errors);
        PlanPrimaryField(plan, documentType, name, existingType, errors);

        var allFieldNames = new HashSet<string>(fieldNames);
        foreach (var reserved in BuiltInContentTypeField.ReservedContentTypeFields)
            allFieldNames.Add(reserved.DeveloperName);
        foreach (var existingField in existingType?.ContentTypeFields ?? [])
            allFieldNames.Add(existingField.DeveloperName!);

        PlanViews(plan, documentType, name, existingType, allFieldNames, existing, usedPaths, errors, warnings);

        if (existingType is not null && plan.Changes.Count > 0)
        {
            plan.Action = PlanAction.Updated;
        }
        return plan;
    }

    private static HashSet<string> PlanFields(
        TypePlan plan,
        SchemaContentType documentType,
        string typeName,
        ContentType? existingType,
        Dictionary<string, Guid> typeIds,
        ExistingSchema existing,
        List<string> errors
    )
    {
        var fields = documentType.Fields ?? [];
        if (existingType is null && fields.Count == 0)
        {
            errors.Add($"{typeName}: a new content type needs at least one field.");
        }

        var existingFields = (existingType?.ContentTypeFields ?? []).ToDictionary(f => f.DeveloperName!);
        existing.DeletedFieldNames.TryGetValue(existingType?.Id ?? Guid.Empty, out var deletedNames);

        var names = new HashSet<string>();
        var order = 0;
        foreach (var field in fields)
        {
            var where = $"{typeName}.{field.DeveloperName}";
            if (!StringExtensions.IsValidDeveloperName(field.DeveloperName ?? ""))
            {
                errors.Add($"{where}: invalid developer name.");
                continue;
            }

            var name = Name(field.DeveloperName);
            where = $"{typeName}.{name}";
            if (BuiltInContentTypeField.ReservedContentTypeFields.Any(r =>
                string.Equals(r.DeveloperName, name, StringComparison.OrdinalIgnoreCase)
            ))
            {
                errors.Add($"{where}: reserved word, cannot be used as a developer name.");
                continue;
            }
            if (!names.Add(name))
            {
                errors.Add($"{where}: appears more than once.");
                continue;
            }
            order++;

            if (string.IsNullOrWhiteSpace(field.Label))
                errors.Add($"{where}: label is required.");

            var fieldType = BaseFieldType.SupportedTypes.FirstOrDefault(t =>
                t.DeveloperName == (field.FieldType ?? "").ToLowerInvariant()
            );
            if (fieldType is null)
            {
                errors.Add($"{where}: unknown field type '{field.FieldType}'.");
                continue;
            }

            if (fieldType.HasChoices)
                errors.AddRange(ChoiceErrors(where, field.Choices ?? []));

            if (fieldType.DeveloperName == BaseFieldType.Repeater.DeveloperName)
            {
                errors.AddRange(
                    RepeaterSubFields.Errors(field.SubFields).Select(e => $"{where}: {e}")
                );
            }

            Guid? relatedId = null;
            if (fieldType.DeveloperName == BaseFieldType.OneToOneRelationship.DeveloperName)
            {
                var related = Name(field.RelatedContentType);
                if (string.IsNullOrWhiteSpace(field.RelatedContentType))
                    errors.Add($"{where}: relatedContentType is required for a relationship field.");
                else if (!typeIds.TryGetValue(related, out var relatedGuid))
                    errors.Add($"{where}: relatedContentType '{field.RelatedContentType}' is not in the schema or on this site.");
                else
                    relatedId = relatedGuid;
            }

            existingFields.TryGetValue(name, out var existingField);
            var fieldPlan = new FieldPlan
            {
                DeveloperName = name,
                Document = field,
                Id = existingField?.Id ?? Guid.NewGuid(),
                Existing = existingField,
                FieldType = fieldType,
                RelatedContentTypeId = relatedId,
                Order = order,
                Action = existingField is null ? PlanAction.Created : PlanAction.Unchanged,
            };

            if (existingField is null)
            {
                if (deletedNames?.Contains(name) == true)
                {
                    errors.Add(
                        $"{where}: a previously deleted field used that developer name. Choose another."
                    );
                }
            }
            else
            {
                if (existingField.FieldType.DeveloperName != fieldType.DeveloperName)
                {
                    errors.Add(
                        $"{where}: the field type cannot change from '{existingField.FieldType.DeveloperName}' to '{fieldType.DeveloperName}'."
                    );
                }
                else if (existingField.RelatedContentTypeId != relatedId && relatedId is not null)
                {
                    errors.Add($"{where}: the related content type cannot change.");
                }

                Track(fieldPlan.Changes, "label", existingField.Label, field.Label);
                Track(fieldPlan.Changes, "description", Text(existingField.Description), Text(field.Description));
                Track(fieldPlan.Changes, "isRequired", existingField.IsRequired, field.IsRequired);
                if (fieldType.HasChoices)
                    Track(fieldPlan.Changes, "choices", Json(ExistingChoices(existingField)), Json(NewChoices(field)));
                if (fieldType.DeveloperName == BaseFieldType.Repeater.DeveloperName)
                    Track(
                        fieldPlan.Changes,
                        "subFields",
                        FieldDefinition.ListToJson(existingField.SubFields),
                        FieldDefinition.ListToJson(SafeNormalize(field.SubFields))
                    );
                if (fieldPlan.Changes.Count > 0)
                    fieldPlan.Action = PlanAction.Updated;
            }

            plan.Fields.Add(fieldPlan);
        }

        // Existing fields the document leaves out keep their place after the ones it lists.
        var unlisted = (existingType?.ContentTypeFields ?? [])
            .Where(f => !names.Contains(f.DeveloperName!))
            .OrderBy(f => f.FieldOrder)
            .ToList();
        var nextOrder = order;
        var unlistedOrders = unlisted.ToDictionary(f => f.Id, _ => ++nextOrder);

        foreach (var fieldPlan in plan.Fields.Where(f => f.Existing is not null))
        {
            if (fieldPlan.Existing!.FieldOrder != fieldPlan.Order)
            {
                fieldPlan.Changes.Add("order");
                fieldPlan.Action = PlanAction.Updated;
            }
        }
        foreach (var field in unlisted)
        {
            plan.UnlistedOrders[field.Id] = unlistedOrders[field.Id];
        }

        return names;
    }

    private static void PlanPrimaryField(
        TypePlan plan,
        SchemaContentType documentType,
        string typeName,
        ContentType? existingType,
        List<string> errors
    )
    {
        if (string.IsNullOrWhiteSpace(documentType.PrimaryField))
        {
            if (existingType is null)
                errors.Add($"{typeName}: primaryField is required for a new content type.");
            else
                plan.PrimaryFieldId = existingType.PrimaryFieldId;
            return;
        }

        var name = Name(documentType.PrimaryField);
        var listed = plan.Fields.FirstOrDefault(f => f.DeveloperName == name);
        var current = listed is null
            ? existingType?.ContentTypeFields.FirstOrDefault(f => f.DeveloperName == name)
            : null;

        BaseFieldType? type = listed?.FieldType ?? current?.FieldType;
        var id = listed?.Id ?? current?.Id;
        if (type is null || id is null)
        {
            errors.Add($"{typeName}: primaryField '{documentType.PrimaryField}' is not a field of this content type.");
            return;
        }
        if (type.DeveloperName != BaseFieldType.SingleLineText.DeveloperName)
        {
            errors.Add($"{typeName}: primaryField must be a single_line_text field.");
            return;
        }

        plan.PrimaryFieldId = id.Value;
        if (existingType is not null && existingType.PrimaryFieldId != id.Value)
            plan.Changes.Add("primaryField");
    }

    private static void PlanViews(
        TypePlan plan,
        SchemaContentType documentType,
        string typeName,
        ContentType? existingType,
        HashSet<string> fieldNames,
        ExistingSchema existing,
        HashSet<string> usedPaths,
        List<string> errors,
        List<string> warnings
    )
    {
        var views = (documentType.Views ?? []).ToList();
        if (existingType is null && views.Count == 0)
        {
            // A content type with no view cannot be listed, so give it the same one a new type gets.
            views.Add(
                new SchemaView
                {
                    DeveloperName = typeName,
                    Label = $"All {documentType.LabelPlural?.ToLower()}",
                    IsPublished = true,
                    RoutePath = usedPaths.Contains(typeName) ? null : typeName,
                    Columns =
                    [
                        BuiltInContentTypeField.PrimaryField.DeveloperName,
                        BuiltInContentTypeField.CreationTime.DeveloperName,
                        BuiltInContentTypeField.Template.DeveloperName,
                    ],
                }
            );
        }

        var accessible = existing
            .Templates.Where(t =>
                !t.IsBaseLayout
                && (
                    existingType is null
                        ? t.AllowAccessForNewContentTypes
                        : t.AccessibleContentTypeIds.Contains(existingType.Id)
                )
            )
            .ToList();
        if (existingType is null && accessible.Count == 0)
        {
            errors.Add(
                $"{typeName}: no web template in the active theme is available to new content types."
            );
        }

        var existingViews = (existingType?.Views ?? []).ToDictionary(v => v.DeveloperName!);
        var names = new HashSet<string>();
        foreach (var view in views)
        {
            var where = $"{typeName}.views.{view.DeveloperName}";
            if (
                !StringExtensions.IsValidDeveloperName(view.DeveloperName ?? "")
                || Name(view.DeveloperName).Length > 64
            )
            {
                errors.Add($"{where}: invalid developer name (at most 64 characters).");
                continue;
            }
            var name = Name(view.DeveloperName);
            where = $"{typeName}.views.{name}";
            if (!names.Add(name))
            {
                errors.Add($"{where}: appears more than once.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(view.Label))
                errors.Add($"{where}: label is required.");
            if (view.DefaultNumberOfItemsPerPage <= 0)
                errors.Add($"{where}: defaultNumberOfItemsPerPage must be greater than 0.");
            if (view.MaxNumberOfItemsPerPage < view.DefaultNumberOfItemsPerPage)
                errors.Add($"{where}: maxNumberOfItemsPerPage cannot be less than defaultNumberOfItemsPerPage.");

            existingViews.TryGetValue(name, out var existingView);
            var viewPlan = new ViewPlan
            {
                DeveloperName = name,
                Document = view,
                Id = existingView?.Id ?? Guid.NewGuid(),
                Existing = existingView,
                Action = existingView is null ? PlanAction.Created : PlanAction.Unchanged,
            };

            var columns = view.Columns ?? [];
            foreach (var column in columns.Where(c => !fieldNames.Contains(c)))
                errors.Add($"{where}: column '{column}' is not a field of {typeName}.");

            foreach (var sort in view.Sort ?? [])
            {
                if (!fieldNames.Contains(sort.DeveloperName ?? ""))
                {
                    errors.Add($"{where}: sort '{sort.DeveloperName}' is not a field of {typeName}.");
                    continue;
                }
                try
                {
                    viewPlan.Sort.Add(
                        new ColumnSortOrder
                        {
                            DeveloperName = sort.DeveloperName,
                            SortOrder = SortOrder.From(sort.Direction ?? ""),
                        }
                    );
                }
                catch (Exception)
                {
                    errors.Add($"{where}: sort direction '{sort.Direction}' must be asc or desc.");
                }
            }

            MapFilter(viewPlan, view, where, fieldNames, errors);
            PlanRoutePath(viewPlan, view, where, typeName, existingView, usedPaths, errors);
            PlanTemplate(viewPlan, view, where, existingView, accessible, existing, errors, warnings);

            if (existingView is not null)
            {
                Track(viewPlan.Changes, "label", existingView.Label, view.Label);
                Track(viewPlan.Changes, "description", Text(existingView.Description), Text(view.Description));
                Track(viewPlan.Changes, "isPublished", existingView.IsPublished, view.IsPublished);
                Track(
                    viewPlan.Changes,
                    "defaultNumberOfItemsPerPage",
                    existingView.DefaultNumberOfItemsPerPage,
                    view.DefaultNumberOfItemsPerPage
                );
                Track(
                    viewPlan.Changes,
                    "maxNumberOfItemsPerPage",
                    existingView.MaxNumberOfItemsPerPage,
                    view.MaxNumberOfItemsPerPage
                );
                Track(
                    viewPlan.Changes,
                    "ignoreClientFilterAndSortQueryParams",
                    existingView.IgnoreClientFilterAndSortQueryParams,
                    view.IgnoreClientFilterAndSortQueryParams
                );
                Track(viewPlan.Changes, "columns", Json(existingView.Columns), Json(columns));
                Track(viewPlan.Changes, "sort", Json(existingView.Sort), Json(viewPlan.Sort));
                Track(viewPlan.Changes, "filter", Json(existingView.Filter), Json(viewPlan.Filter));
                if (viewPlan.RoutePath is not null)
                    viewPlan.Changes.Add("routePath");
                if (viewPlan.TemplateId is not null)
                    viewPlan.Changes.Add("template");
                if (viewPlan.Changes.Count > 0)
                    viewPlan.Action = PlanAction.Updated;
            }

            plan.Views.Add(viewPlan);
        }
    }

    private static void MapFilter(
        ViewPlan viewPlan,
        SchemaView view,
        string where,
        HashSet<string> fieldNames,
        List<string> errors
    )
    {
        foreach (var problem in FilterConditionTree.Problems((view.Filter ?? []).ToList()))
            errors.Add($"{where}: filter: {problem}");

        var index = 0;
        foreach (var condition in view.Filter ?? [])
        {
            index++;
            try
            {
                var type = FilterConditionType.From(condition.Type ?? "");
                var isCondition =
                    type.DeveloperName == FilterConditionType.FilterCondition.DeveloperName;
                ConditionOperator? conditionOperator = null;

                if (isCondition)
                {
                    if (string.IsNullOrEmpty(condition.Field))
                    {
                        errors.Add($"{where}: filter condition {index} is missing a field.");
                        continue;
                    }
                    if (!fieldNames.Contains(condition.Field))
                    {
                        errors.Add($"{where}: filter condition {index} uses '{condition.Field}', which is not a field of the content type.");
                        continue;
                    }
                    conditionOperator = ConditionOperator.From(condition.ConditionOperator ?? "");
                    if (
                        !ConditionOperator.OperatorsWithoutValues.Contains(conditionOperator)
                        && string.IsNullOrEmpty(condition.Value)
                    )
                    {
                        errors.Add($"{where}: filter condition {index} on '{condition.Field}' is missing a value.");
                        continue;
                    }
                }

                viewPlan.Filter.Add(
                    new FilterCondition
                    {
                        Id = condition.Id,
                        ParentId = condition.ParentId,
                        Type = type,
                        GroupOperator = string.IsNullOrEmpty(condition.GroupOperator)
                            ? null
                            : BooleanOperator.From(condition.GroupOperator),
                        Field = condition.Field,
                        ConditionOperator =
                            conditionOperator
                            ?? (
                                string.IsNullOrEmpty(condition.ConditionOperator)
                                    ? null
                                    : ConditionOperator.From(condition.ConditionOperator)
                            ),
                        Value = condition.Value,
                    }
                );
            }
            catch (Exception)
            {
                errors.Add($"{where}: filter condition {index} has an unknown type or operator.");
            }
        }
    }

    private static void PlanRoutePath(
        ViewPlan viewPlan,
        SchemaView view,
        string where,
        string typeName,
        View? existingView,
        HashSet<string> usedPaths,
        List<string> errors
    )
    {
        var ownPath = existingView?.Route?.Path;
        if (string.IsNullOrWhiteSpace(view.RoutePath))
        {
            if (existingView is null)
            {
                var viewName = viewPlan.DeveloperName;
                var path = $"{typeName}/{viewName}".Truncate(200, string.Empty).ToUrlSlug();
                if (usedPaths.Contains(path))
                {
                    path = $"{typeName}/{(ShortGuid)viewPlan.Id}-{viewName}"
                        .Truncate(200, string.Empty)
                        .ToUrlSlug();
                }
                usedPaths.Add(path);
                viewPlan.RoutePath = path;
            }
            return;
        }

        var slug = view.RoutePath.ToUrlSlug();
        if (string.IsNullOrWhiteSpace(slug) || !slug.IsValidRoutePath())
        {
            errors.Add($"{where}: routePath '{view.RoutePath}' is not a valid route path.");
            return;
        }
        if (slug.IsProtectedRoutePath())
        {
            errors.Add($"{where}: routePath '{slug}' begins with a protected path.");
            return;
        }
        if (string.Equals(slug, ownPath, StringComparison.OrdinalIgnoreCase))
            return;
        if (usedPaths.Contains(slug))
        {
            errors.Add($"{where}: routePath '{slug}' is already in use.");
            return;
        }

        usedPaths.Add(slug);
        viewPlan.RoutePath = slug;
    }

    private static void PlanTemplate(
        ViewPlan viewPlan,
        SchemaView view,
        string where,
        View? existingView,
        List<TemplateInfo> accessible,
        ExistingSchema existing,
        List<string> errors,
        List<string> warnings
    )
    {
        TemplateInfo? named = null;
        if (!string.IsNullOrWhiteSpace(view.Template))
        {
            named = accessible.FirstOrDefault(t =>
                string.Equals(t.DeveloperName, view.Template, StringComparison.OrdinalIgnoreCase)
            );
            var alreadyUsed =
                existingView is not null
                && existing.ViewRelations.TryGetValue(existingView.Id, out var current)
                && existing.Templates.Any(t =>
                    t.Id == current.WebTemplateId
                    && string.Equals(t.DeveloperName, view.Template, StringComparison.OrdinalIgnoreCase)
                );
            if (named is null && !alreadyUsed)
            {
                warnings.Add(
                    $"{where}: template '{view.Template}' is not available in the active theme for this content type; "
                        + (existingView is null ? "using the default list template." : "keeping the current template.")
                );
            }
        }

        if (existingView is null)
        {
            var chosen =
                named
                ?? accessible.FirstOrDefault(t =>
                    t.DeveloperName == BuiltInWebTemplate.ContentItemListViewPage.DeveloperName
                )
                ?? accessible.FirstOrDefault();
            if (chosen is null)
                errors.Add($"{where}: no list template is available for this content type.");
            else
                viewPlan.TemplateId = chosen.Id;
            return;
        }

        existing.ViewRelations.TryGetValue(existingView.Id, out var relation);
        if (named is not null && relation?.WebTemplateId != named.Id)
            viewPlan.TemplateId = named.Id;
    }

    private static IEnumerable<string> ChoiceErrors(string where, IReadOnlyList<SchemaChoice> choices)
    {
        if (choices.Count == 0)
        {
            yield return $"{where}: at least one choice is required.";
            yield break;
        }
        if (choices.Any(c => string.IsNullOrWhiteSpace(c.Label)))
            yield return $"{where}: choice labels cannot be empty.";

        var names = choices.Select(c => Name(c.DeveloperName)).ToList();
        if (names.Any(string.IsNullOrEmpty))
            yield return $"{where}: choice developer names cannot be empty.";
        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            yield return $"{where}: choice developer names must be unique; duplicates: {string.Join(", ", duplicates)}.";
    }

    /// <summary>The choices as they would be stored, for comparing with the stored ones.</summary>
    public static IReadOnlyList<ContentTypeFieldChoice> NewChoices(SchemaField field) =>
        (field.Choices ?? [])
            .Select(c => new ContentTypeFieldChoice
            {
                DeveloperName = Name(c.DeveloperName),
                Label = c.Label,
                Disabled = c.Disabled,
            })
            .ToList();

    private static IReadOnlyList<ContentTypeFieldChoice> ExistingChoices(ContentTypeField field) =>
        field.Choices.ToList();

    /// <summary>The sub-fields as they would be stored; an invalid set compares as it was sent.</summary>
    public static IReadOnlyList<FieldDefinition> SafeNormalize(IReadOnlyList<FieldDefinition>? subFields)
    {
        if (subFields is null || RepeaterSubFields.Errors(subFields).Count > 0)
            return subFields ?? [];
        return RepeaterSubFields.Normalize(subFields);
    }

    private static string Name(string? developerName) => (developerName ?? string.Empty).ToDeveloperName();

    private static string Text(string? value) => value ?? string.Empty;

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static void Track<T>(List<string> changes, string property, T current, T incoming)
    {
        if (!EqualityComparer<T>.Default.Equals(current, incoming))
            changes.Add(property);
    }
}
