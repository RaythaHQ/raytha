using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Raytha.Domain.Entities;

public class View : BaseAuditableEntity
{
    public string? Label { get; set; }
    public string? DeveloperName { get; set; }
    public string? Description { get; set; }
    public Guid ContentTypeId { get; set; }
    public virtual ContentType? ContentType { get; set; }
    public Guid WebTemplateId { get; set; }
    public virtual WebTemplate WebTemplate { get; set; }
    public Guid RouteId { get; set; }
    public virtual Route Route { get; set; }
    public bool IsPublished { get; set; }
    public string? _Columns { get; set; }
    public string? _Filter { get; set; }
    public string? _Sort { get; set; }
    public virtual ICollection<User> UserFavorites { get; set; } = new List<User>();

    [NotMapped]
    public IEnumerable<string> Columns
    {
        get { return JsonSerializer.Deserialize<IEnumerable<string>>(_Columns ?? "[]") ?? new List<string>(); }
        set { _Columns = JsonSerializer.Serialize(value); }
    }

    [NotMapped]
    public IEnumerable<ColumnSortOrder> Sort
    {
        get { return JsonSerializer.Deserialize<IEnumerable<ColumnSortOrder>>(_Sort ?? "[]") ?? new List<ColumnSortOrder>(); }
        set { _Sort = JsonSerializer.Serialize(value); }
    }

    [NotMapped]
    public IEnumerable<FilterCondition> Filter
    {
        get { return JsonSerializer.Deserialize<IEnumerable<FilterCondition>>(_Filter ?? "[]") ?? new List<FilterCondition>(); }
        set { _Filter = JsonSerializer.Serialize(value); }
    }
}

