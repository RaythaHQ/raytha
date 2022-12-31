using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Interfaces;

public interface IRaythaDbJsonQueryEngine
{
    ContentItem FirstOrDefault(Guid entityId);
    IEnumerable<ContentItem> QueryContentItems(Guid contentTypeId,
                                               string[] searchOnColumns,
                                               string search,
                                               string[] filters,
                                               int pageSize,
                                               int pageNumber,
                                               string orderBy);
    int CountContentItems(Guid contentTypeId,
                          string[] searchOnColumns,
                          string search,
                          string[] filters);
}
