namespace Raytha.Domain.Entities;

public class MediaItem : BaseAuditableEntity
{
    public long Length { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string FileStorageProvider { get; set; }
    public string ObjectKey { get; set; }
}
