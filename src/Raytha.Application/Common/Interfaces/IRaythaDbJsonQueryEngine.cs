using System.Data;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Interfaces;

public interface IRaythaDbJsonQueryEngine
{
    ContentItem FirstOrDefault(Guid entityId);
    IEnumerable<ContentItem> QueryContentItems(
        Guid contentTypeId,
        string[] searchOnColumns,
        string search,
        string[] filters,
        int pageSize,
        int pageNumber,
        string orderBy,
        IDbTransaction transaction = null
    );

    IEnumerable<ContentItem> QueryAllContentItemsAsTransaction(
        Guid contentTypeId,
        string[] searchOnColumns,
        string search,
        string[] filters,
        string orderBy
    );

    int CountContentItems(
        Guid contentTypeId,
        string[] searchOnColumns,
        string search,
        string[] filters,
        IDbTransaction transaction = null
    );
}
