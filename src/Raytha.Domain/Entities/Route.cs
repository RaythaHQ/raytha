using System.ComponentModel.DataAnnotations.Schema;

namespace Raytha.Domain.Entities;

public class Route : BaseEntity
{
    public string Path { get; set; }

    public Guid ContentItemId { get; set; }
    public virtual ContentItem ContentItem { get; set; }

    public Guid ViewId { get; set; }
    public virtual View View { get; set; }
}
