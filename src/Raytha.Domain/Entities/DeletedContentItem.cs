namespace Raytha.Domain.Entities;

public class DeletedContentItem : BaseAuditableEntity
{
    public string? _PublishedContent { get; set; }
    public string? PrimaryField { get; set; }
    public Guid ContentTypeId { get; set; }
    public Guid OriginalContentItemId { get; set; }
    public string RoutePath { get; set; }
    public required string WebTemplateIdsJson { get; init; }

    public virtual ContentType? ContentType { get; set; }
}