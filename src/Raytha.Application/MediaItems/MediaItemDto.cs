using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.MediaItems;

public record MediaItemDto : BaseAuditableEntityDto
{
    public long Length { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public string FileStorageProvider { get; init; }
    public string ObjectKey { get; init; }

    public static Expression<Func<MediaItem, MediaItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static MediaItemDto GetProjection(MediaItem entity)
    {
        return new MediaItemDto
        {
            Id = entity.Id,
            Length = entity.Length,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileStorageProvider = entity.FileStorageProvider,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            ObjectKey = entity.ObjectKey
        };
    }
}
