using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentTypes;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.Views
{
    public record ViewDto : BaseAuditableEntityDto
    {
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public AuditableUserDto? LastModifierUser { get; init; }
        public IEnumerable<string> Columns { get; init; } = new List<string>();
        public IEnumerable<ColumnSortOrder> Sort { get; init; } = new List<ColumnSortOrder>();
        public IEnumerable<FilterCondition> Filter { get; init; } = new List<FilterCondition>();
        public int DefaultNumberOfItemsPerPage { get; init; }
        public int MaxNumberOfItemsPerPage { get; init; }
        public bool IgnoreClientFilterAndSortQueryParams { get; init; }
        public ContentTypeDto? ContentType { get; init; }
        public ShortGuid ContentTypeId { get; init; }
        public ShortGuid? RouteId { get; init; }
        public string RoutePath { get; init; }
        public bool IsPublished { get; init; }

        public static Expression<Func<View, ViewDto>> GetProjection()
        {
            return entity => GetProjection(entity);
        }

        public static ViewDto GetProjection(View entity)
        {
            if (entity == null)
                return null;

            return new ViewDto
            {
                Id = entity.Id,
                Label = entity.Label,
                DeveloperName = entity.DeveloperName,
                Description = entity.Description,
                CreatorUserId = entity.CreatorUserId,
                CreationTime = entity.CreationTime,
                LastModificationTime = entity.LastModificationTime,
                LastModifierUserId = entity.LastModifierUserId,
                LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
                Columns = entity.Columns,
                ContentType = ContentTypeDto.GetProjection(entity.ContentType),
                ContentTypeId = entity.ContentTypeId,
                Filter = entity.Filter,
                Sort = entity.Sort,
                RouteId = entity.RouteId,
                RoutePath = entity.Route.Path,
                IsPublished = entity.IsPublished,
                IgnoreClientFilterAndSortQueryParams = entity.IgnoreClientFilterAndSortQueryParams,
                DefaultNumberOfItemsPerPage = entity.DefaultNumberOfItemsPerPage,
                MaxNumberOfItemsPerPage = entity.MaxNumberOfItemsPerPage
            };
        }
    }
}
