using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContentItemDto>>>
    {
        private readonly IRaythaDbJsonQueryEngine _db;
        private readonly IRaythaDbContext _entityFrameworkDb;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
        public Handler(IRaythaDbJsonQueryEngine db, IRaythaDbContext entityFrameworkDb, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _db = db;
            _entityFrameworkDb = entityFrameworkDb;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }

        public async Task<IQueryResponseDto<ListResultDto<ContentItemDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            IEnumerable<ContentItemDto> items;
            int count = 0;
            if (request.ViewId.HasValue && request.ViewId.Value != ShortGuid.Empty)
            {
                View view = _entityFrameworkDb.Views
                    .Include(p => p.ContentType)
                    .FirstOrDefault(p => p.Id == request.ViewId.Value.Guid);

                if (view == null)
                    throw new NotFoundException("View", request.ViewId);

                _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(view.ContentType.DeveloperName);

                var searchOnColumns = GetSearchForView(view);
                var filters = GetFiltersForView(view, request);
                string finalOrderBy = GetSortForView(view, request);
                var queryResult = _db.QueryContentItems(view.ContentTypeId,
                                                  searchOnColumns,
                                                  request.Search,
                                                  filters,
                                                  GetPageSizeForView(view, request),
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
                var queryResult = _db.QueryContentItems(contentType.Id, null, request.Search, filters, GetPageSizeForView(request), request.PageNumber, finalOrderBy).ToList();
                count = _db.CountContentItems(contentType.Id, null, request.Search, filters);
                items = queryResult.Select(p => ContentItemDto.GetProjection(p));
            }
            return new QueryResponseDto<ListResultDto<ContentItemDto>>(new ListResultDto<ContentItemDto>(items, count));
        }

        protected string[] GetSearchForView(View view)
        {
            return view.Columns != null ? view.Columns.ToArray() : new string[0];
        }

        protected string[] GetFiltersForView(View view, Query request)
        {
            var conditionToODataUtility = new FilterConditionToODataUtility(view.ContentType);
            var oDataFromFilter = conditionToODataUtility.ToODataFilter(view.Filter);
            return view.IgnoreClientFilterAndSortQueryParams
                ? (new string[] { oDataFromFilter })
                : (new string[] { oDataFromFilter, request.Filter });
        }

        protected string GetSortForView(View view, Query request)
        {
            string finalOrderBy = $"{BuiltInContentTypeField.CreationTime.DeveloperName} {SortOrder.DESCENDING}";
            var viewOrderBy = view.Sort.Select(p => $"{p.DeveloperName} {p.SortOrder.DeveloperName}").ToList();
            finalOrderBy = view.IgnoreClientFilterAndSortQueryParams
                ? viewOrderBy.Any() ? string.Join(",", viewOrderBy) : finalOrderBy
                : !string.IsNullOrWhiteSpace(request.OrderBy) ? request.OrderBy : viewOrderBy.Any() ? string.Join(",", viewOrderBy) : finalOrderBy;

            return finalOrderBy;
        }

        protected int GetPageSizeForView(View view, Query request)
        {
            if (request.PageSize <= 0)
                return view.DefaultNumberOfItemsPerPage;
            else if (request.PageSize > view.MaxNumberOfItemsPerPage)
                return view.MaxNumberOfItemsPerPage;
            else
                return request.PageSize;
        }

        protected int GetPageSizeForView(Query request)
        {
            if (request.PageSize <= 0)
                return View.DEFAULT_NUMBER_OF_ITEMS_PER_PAGE;
            else if (request.PageSize > View.DEFAULT_MAX_ITEMS_PER_PAGE)
                return View.DEFAULT_MAX_ITEMS_PER_PAGE;
            else
                return request.PageSize;
        }
    }
}
