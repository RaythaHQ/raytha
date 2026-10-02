using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes.Queries;

/// <summary>
/// The whole content model as one <see cref="SchemaDocument"/>: every content type with its
/// fields, choices, and views. Ordered by developer name, with fields in display order, so the
/// same model always exports the same text.
/// </summary>
public class ExportSchema
{
    public record Query : IRequest<IQueryResponseDto<SchemaDocument>>;

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SchemaDocument>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SchemaDocument>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var contentTypes = await _db
                .ContentTypes.AsNoTracking()
                .Include(ct => ct.ContentTypeFields)
                .Include(ct => ct.Views)
                .ThenInclude(v => v.Route)
                .OrderBy(ct => ct.DeveloperName)
                .ToListAsync(cancellationToken);

            var activeThemeId = await _db
                .OrganizationSettings.Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var viewTemplateNames = (
                await _db
                    .WebTemplateViewRelations.AsNoTracking()
                    .Where(r => r.WebTemplate!.ThemeId == activeThemeId)
                    .Select(r => new { r.ViewId, r.WebTemplate!.DeveloperName })
                    .ToListAsync(cancellationToken)
            )
                .GroupBy(r => r.ViewId)
                .ToDictionary(g => g.Key, g => g.First().DeveloperName);

            var developerNameById = contentTypes.ToDictionary(ct => ct.Id, ct => ct.DeveloperName!);

            var document = new SchemaDocument
            {
                ContentTypes = contentTypes
                    .Select(ct => ToDocument(ct, developerNameById, viewTemplateNames))
                    .ToList(),
            };
            return new QueryResponseDto<SchemaDocument>(document);
        }

        private static SchemaContentType ToDocument(
            ContentType contentType,
            Dictionary<Guid, string> developerNameById,
            Dictionary<Guid, string?> viewTemplateNames
        )
        {
            var fields = contentType.ContentTypeFields.OrderBy(f => f.FieldOrder).ToList();
            return new SchemaContentType
            {
                DeveloperName = contentType.DeveloperName!,
                LabelPlural = contentType.LabelPlural ?? string.Empty,
                LabelSingular = contentType.LabelSingular ?? string.Empty,
                Description = contentType.Description ?? string.Empty,
                DefaultRouteTemplate = contentType.DefaultRouteTemplate ?? string.Empty,
                PrimaryField = fields
                    .FirstOrDefault(f => f.Id == contentType.PrimaryFieldId)
                    ?.DeveloperName,
                Fields = fields.Select(f => ToDocument(f, developerNameById)).ToList(),
                Views = contentType
                    .Views.OrderBy(v => v.DeveloperName)
                    .Select(v => ToDocument(v, viewTemplateNames))
                    .ToList(),
            };
        }

        private static SchemaField ToDocument(
            ContentTypeField field,
            Dictionary<Guid, string> developerNameById
        ) =>
            new()
            {
                DeveloperName = field.DeveloperName!,
                Label = field.Label ?? string.Empty,
                Description = field.Description ?? string.Empty,
                FieldType = field.FieldType.DeveloperName,
                IsRequired = field.IsRequired,
                Choices = field.FieldType.HasChoices
                    ? field
                        .Choices.Select(c => new SchemaChoice
                        {
                            DeveloperName = c.DeveloperName ?? string.Empty,
                            Label = c.Label ?? string.Empty,
                            Disabled = c.Disabled,
                        })
                        .ToList()
                    : [],
                RelatedContentType =
                    field.RelatedContentTypeId is { } relatedId
                    && developerNameById.TryGetValue(relatedId, out var relatedName)
                        ? relatedName
                        : null,
                SubFields =
                    field.FieldType.DeveloperName == BaseFieldType.Repeater.DeveloperName
                        ? field.SubFields
                        : [],
            };

        private static SchemaView ToDocument(View view, Dictionary<Guid, string?> viewTemplateNames) =>
            new()
            {
                DeveloperName = view.DeveloperName!,
                Label = view.Label ?? string.Empty,
                Description = view.Description ?? string.Empty,
                IsPublished = view.IsPublished,
                RoutePath = view.Route?.Path,
                Template = viewTemplateNames.GetValueOrDefault(view.Id),
                Columns = view.Columns.ToList(),
                Sort = view
                    .Sort.Select(s => new SchemaSort
                    {
                        DeveloperName = s.DeveloperName ?? string.Empty,
                        Direction = s.SortOrder?.DeveloperName ?? "asc",
                    })
                    .ToList(),
                Filter = view
                    .Filter.Select(c => new Views.FilterConditionInputDto
                    {
                        Id = c.Id,
                        ParentId = c.ParentId,
                        Type = c.Type?.DeveloperName ?? string.Empty,
                        GroupOperator = c.GroupOperator?.DeveloperName ?? string.Empty,
                        Field = c.Field,
                        ConditionOperator = c.ConditionOperator?.DeveloperName ?? string.Empty,
                        Value = c.Value,
                    })
                    .ToList(),
                DefaultNumberOfItemsPerPage = view.DefaultNumberOfItemsPerPage,
                MaxNumberOfItemsPerPage = view.MaxNumberOfItemsPerPage,
                IgnoreClientFilterAndSortQueryParams = view.IgnoreClientFilterAndSortQueryParams,
            };
    }
}
