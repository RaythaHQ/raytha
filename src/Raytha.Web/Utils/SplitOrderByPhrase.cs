using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;
using System.Linq;

namespace Raytha.Web.Utils;

class SplitOrderByPhrase
{
    public string PropertyName { get; set; }
    public string Direction { get; set; }
    public static SplitOrderByPhrase From(string orderByPhrase)
    {
        try
        {
            var firstSortOrderItem = orderByPhrase.Split(",").Select(p => p.Trim()).ToArray().First();
            var orderBySplit = firstSortOrderItem.Split(" ").Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            var s = SortOrder.From(orderBySplit[1]);
            return new SplitOrderByPhrase { PropertyName = orderBySplit[0], Direction = s.DeveloperName };
        }
        catch (SortOrderNotFoundException)
        {
            return null;
        }
    }
}
