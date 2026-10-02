using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks;

public record WebhookDto : BaseAuditableEntityDto
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string[] SubscribedEvents { get; init; } = Array.Empty<string>();
    public int MaxAttempts { get; init; }
    public int TimeoutSeconds { get; init; }

    public static Expression<Func<Webhook, WebhookDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebhookDto GetProjection(Webhook entity)
    {
        return new WebhookDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Url = entity.Url,
            Description = entity.Description,
            IsActive = entity.IsActive,
            SubscribedEvents = entity.SubscribedEvents,
            MaxAttempts = entity.MaxAttempts,
            TimeoutSeconds = entity.TimeoutSeconds,
            CreationTime = entity.CreationTime,
            CreatorUserId = entity.CreatorUserId,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}

/// <summary>Returned once at creation so the caller can capture the signing secret.</summary>
public record CreatedWebhookDto
{
    public CSharpVitamins.ShortGuid Id { get; init; }
    public string Secret { get; init; } = string.Empty;
}
