using Raytha.Application.Common.Models;
using System.Linq.Dynamic.Core;

namespace Raytha.Application.Common.Utils;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaginationInput<T>(this IQueryable<T> source, GetPagedEntitiesInputDto input) where T : class
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (input == null) throw new ArgumentNullException(nameof(input));

        IEnumerable<GetPagedEntitiesInputDtoOrderByItem> orderByItems = input.GetOrderByItems().Where(p => typeof(T).GetProperty(p.OrderByPropertyName) != null);

        if (orderByItems.Any())
        {
            var orderByString = string.Join(", ", orderByItems);
            source = source.OrderBy(orderByString);
        }

        var pageSize = Math.Clamp(input.PageSize, 1, int.MaxValue);
        var pageNumber = Math.Max(input.PageNumber, 1);

        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}