using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentItems.Queries;

public class GetContentItems
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ContentItemDto>>>
    {
        public ShortGuid? ViewId { get; init; }

        public string? ContentType { get; init; }
        public string? Filter { get; init; }
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<ContentItemDto>>>
    {
        private readonly IRaythaDbJsonQueryEngine _db;
        private readonly IRaythaDbContext _entityFrameworkDb;
        public Handler(IRaythaDbJsonQueryEngine db, IRaythaDbContext entityFrameworkDb)
        {
            _db = db;
            _entityFrameworkDb = entityFrameworkDb;
        }
        protected override IQueryResponseDto<ListResultDto<ContentItemDto>> Handle(Query request)
        {
            IEnumerable<ContentItemDto> items;
            int count = 0;
            if (request.ViewId.HasValue && request.ViewId.Value != ShortGuid.Empty)
            {
                View view = _entityFrameworkDb.Views
                    .FirstOrDefault(p => p.Id == request.ViewId.Value.Guid);

                if (view == null)
                    throw new NotFoundException("View", request.ViewId);

                var searchOnColumns = view.Columns != null ? view.Columns.ToArray() : new string[0];
                var viewFilter = view.Filter;
                var conditionToODataUtility = new FilterConditionToODataUtility(view.ContentType);
                var oDataFromFilter = conditionToODataUtility.ToODataFilter(view.Filter);
                var filters = new string[] { oDataFromFilter, request.Filter };
                var viewOrderBy = view.Sort.Select(p => $"{p.DeveloperName} {p.SortOrder.DeveloperName}").ToList();
                string finalOrderBy = !string.IsNullOrWhiteSpace(request.OrderBy) ? request.OrderBy : viewOrderBy.Any() ? string.Join(",", viewOrderBy) : string.Empty;

                if (string.IsNullOrWhiteSpace(finalOrderBy))
                    finalOrderBy = $"{BuiltInContentTypeField.CreationTime.DeveloperName} {SortOrder.DESCENDING}";
                var queryResult = _db.QueryContentItems(view.ContentTypeId,
                                                  searchOnColumns,
                                                  request.Search,
                                                  filters,
                                                  request.PageSize,
                                                  request.PageNumber,
                                                  finalOrderBy);
                count = _db.CountContentItems(view.ContentTypeId, searchOnColumns, request.Search, filters);
                items = queryResult.Select(p => ContentItemDto.GetProjection(p));
            }
            else
            {
                ContentType contentType = _entityFrameworkDb.ContentTypes
                    .FirstOrDefault(p => p.DeveloperName == request.ContentType.ToDeveloperName());

                if (contentType == null)
                    throw new NotFoundException("Content Type", request.ContentType.ToDeveloperName());

                var conditionToODataUtility = new FilterConditionToODataUtility(contentType);
                var filters = new string[] { request.Filter };
                string finalOrderBy = !string.IsNullOrWhiteSpace(request.OrderBy) ? request.OrderBy : $"{BuiltInContentTypeField.CreationTime.DeveloperName} {SortOrder.DESCENDING}";
                var queryResult = _db.QueryContentItems(contentType.Id, null, request.Search, filters, request.PageSize, request.PageNumber, finalOrderBy).ToList();
                count = _db.CountContentItems(contentType.Id, null, request.Search, filters);
                items = queryResult.Select(p => ContentItemDto.GetProjection(p));
            }
            return new QueryResponseDto<ListResultDto<ContentItemDto>>(new ListResultDto<ContentItemDto>(items, count));
        }
    }
}
