using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using static Raytha.Application.ContentTypes.SchemaImportPlanner;

namespace Raytha.Application.ContentTypes.Commands;

/// <summary>
/// Applies a <see cref="SchemaDocument"/> to the site: content types, fields, choices, and views
/// are created when missing and brought in line when present, matched by developer name. Nothing
/// is deleted, and a field's type never changes. All of it is applied together or none of it, and
/// a dry run reports what would change without applying anything.
/// </summary>
public class ImportSchema
{
    public record Command : LoggableRequest<CommandResponseDto<ImportResult>>
    {
        public SchemaDocument Schema { get; init; } = new();
        public bool DryRun { get; init; }
    }

    public record ImportResult
    {
        public bool DryRun { get; init; }
        public int Created { get; init; }
        public int Updated { get; init; }
        public int Unchanged { get; init; }

        /// <summary>Each content type, field, and view that is or would be created or updated.</summary>
        public IReadOnlyList<SchemaChange> Changes { get; init; } = [];
        public IReadOnlyList<string> Warnings { get; init; } = [];
    }

    /// <param name="Kind"><c>content_type</c>, <c>field</c>, or <c>view</c>.</param>
    /// <param name="Name">The field or view developer name; null for a content type.</param>
    /// <param name="Action"><c>created</c> or <c>updated</c>.</param>
    /// <param name="Details">For an update, the properties that differ.</param>
    public record SchemaChange(
        string Kind,
        string ContentType,
        string? Name,
        string Action,
        IReadOnlyList<string> Details
    );

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Schema is null)
                        {
                            context.AddFailure("Schema", "The schema is required.");
                            return;
                        }

                        var plan = SchemaImportPlanner.Plan(
                            request.Schema,
                            SchemaImportSnapshot.Load(db)
                        );
                        foreach (var error in plan.Errors)
                        {
                            context.AddFailure("Schema", error);
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ImportResult>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ImportResult>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var existing = SchemaImportSnapshot.Load(_db);
            var plan = SchemaImportPlanner.Plan(request.Schema, existing);
            var result = Describe(plan, request.DryRun);

            if (!request.DryRun && result.Changes.Count > 0)
            {
                await Apply(plan, existing, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponseDto<ImportResult>(result);
        }

        private async Task Apply(
            ImportPlan plan,
            ExistingSchema existing,
            CancellationToken cancellationToken
        )
        {
            foreach (var typePlan in plan.Types)
            {
                var document = typePlan.Document;
                var isNew = typePlan.Existing is null;

                var entity = typePlan.Existing ?? new ContentType { Id = typePlan.Id };
                entity.DeveloperName = typePlan.DeveloperName;
                entity.LabelPlural = document.LabelPlural;
                entity.LabelSingular = document.LabelSingular;
                entity.Description = document.Description ?? string.Empty;
                entity.DefaultRouteTemplate = document.DefaultRouteTemplate;
                entity.PrimaryFieldId = typePlan.PrimaryFieldId;
                if (isNew)
                {
                    _db.ContentTypes.Add(entity);
                }

                foreach (var fieldPlan in typePlan.Fields)
                {
                    ApplyField(typePlan, fieldPlan);
                }
                foreach (var (fieldId, order) in typePlan.UnlistedOrders)
                {
                    entity.ContentTypeFields.First(f => f.Id == fieldId).FieldOrder = order;
                }

                if (isNew)
                {
                    await ContentTypeProvisioning.GrantDefaultTemplateAccessAsync(
                        _db,
                        typePlan.Id,
                        cancellationToken
                    );
                    ContentTypeProvisioning.GrantRolePermissions(_db, typePlan.Id);
                }

                foreach (var viewPlan in typePlan.Views)
                {
                    await ApplyView(typePlan, viewPlan, existing, cancellationToken);
                }
            }
        }

        private void ApplyField(TypePlan typePlan, FieldPlan fieldPlan)
        {
            var document = fieldPlan.Document;
            var fieldType = fieldPlan.FieldType;

            var field = fieldPlan.Existing ?? new ContentTypeField { Id = fieldPlan.Id };
            field.ContentTypeId = typePlan.Id;
            field.DeveloperName = fieldPlan.DeveloperName;
            field.FieldType = fieldType;
            field.Label = document.Label;
            field.Description = document.Description ?? string.Empty;
            field.IsRequired = document.IsRequired;
            field.FieldOrder = fieldPlan.Order;
            field.RelatedContentTypeId = fieldPlan.RelatedContentTypeId;
            field.Choices = fieldType.HasChoices ? NewChoices(document) : [];
            field.SubFields =
                fieldType.DeveloperName == BaseFieldType.Repeater.DeveloperName
                    ? RepeaterSubFields.Normalize(document.SubFields)
                    : [];

            if (fieldPlan.Existing is null)
            {
                _db.ContentTypeFields.Add(field);
            }
        }

        private async Task ApplyView(
            TypePlan typePlan,
            ViewPlan viewPlan,
            ExistingSchema existing,
            CancellationToken cancellationToken
        )
        {
            var document = viewPlan.Document;
            var isNew = viewPlan.Existing is null;

            var view = viewPlan.Existing ?? new View { Id = viewPlan.Id };
            view.ContentTypeId = typePlan.Id;
            view.DeveloperName = viewPlan.DeveloperName;
            view.Label = document.Label;
            view.Description = document.Description ?? string.Empty;
            view.IsPublished = document.IsPublished;
            view.DefaultNumberOfItemsPerPage = document.DefaultNumberOfItemsPerPage;
            view.MaxNumberOfItemsPerPage = document.MaxNumberOfItemsPerPage;
            view.IgnoreClientFilterAndSortQueryParams = document.IgnoreClientFilterAndSortQueryParams;
            view.Columns = (document.Columns ?? []).ToList();
            view.Sort = viewPlan.Sort;
            view.Filter = viewPlan.Filter;

            if (isNew)
            {
                view.Route = new Route { ViewId = view.Id, Path = viewPlan.RoutePath! };
                _db.Views.Add(view);
            }
            else if (viewPlan.RoutePath is not null)
            {
                view.Route.Path = viewPlan.RoutePath;
            }

            if (viewPlan.TemplateId is { } templateId)
            {
                if (existing.ViewRelations.TryGetValue(view.Id, out var relation))
                {
                    relation.WebTemplateId = templateId;
                }
                else
                {
                    await _db.WebTemplateViewRelations.AddAsync(
                        new WebTemplateViewRelation
                        {
                            Id = Guid.NewGuid(),
                            ViewId = view.Id,
                            WebTemplateId = templateId,
                        },
                        cancellationToken
                    );
                }
            }
        }

        private static ImportResult Describe(ImportPlan plan, bool dryRun)
        {
            var changes = new List<SchemaChange>();
            var created = 0;
            var updated = 0;
            var unchanged = 0;

            void Record(string kind, string contentType, string? name, PlanAction action, List<string> details)
            {
                switch (action)
                {
                    case PlanAction.Created:
                        created++;
                        changes.Add(new SchemaChange(kind, contentType, name, "created", []));
                        break;
                    case PlanAction.Updated:
                        updated++;
                        changes.Add(new SchemaChange(kind, contentType, name, "updated", details));
                        break;
                    default:
                        unchanged++;
                        break;
                }
            }

            foreach (var type in plan.Types)
            {
                Record("content_type", type.DeveloperName, null, type.Action, type.Changes);
                foreach (var field in type.Fields)
                    Record("field", type.DeveloperName, field.DeveloperName, field.Action, field.Changes);
                foreach (var view in type.Views)
                    Record("view", type.DeveloperName, view.DeveloperName, view.Action, view.Changes);
            }

            return new ImportResult
            {
                DryRun = dryRun,
                Created = created,
                Updated = updated,
                Unchanged = unchanged,
                Changes = changes,
                Warnings = plan.Warnings,
            };
        }
    }
}
