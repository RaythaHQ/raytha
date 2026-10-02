using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks;

public record WebhookDeliveryDto : BaseEntityDto
{
    public ShortGuid WebhookId { get; init; }
    public string WebhookName { get; init; } = string.Empty;
    public string EventName { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public int? ResponseCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public long? DurationMs { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? CompletionTime { get; init; }

    public static Expression<Func<WebhookDelivery, WebhookDeliveryDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebhookDeliveryDto GetProjection(WebhookDelivery entity)
    {
        return new WebhookDeliveryDto
        {
            Id = entity.Id,
            WebhookId = entity.WebhookId,
            WebhookName = entity.Webhook != null ? entity.Webhook.Name : string.Empty,
            EventName = entity.EventName,
            Payload = entity.Payload,
            Status = entity.Status.DeveloperName,
            AttemptCount = entity.AttemptCount,
            LastAttemptAt = entity.LastAttemptAt,
            NextRetryAt = entity.NextRetryAt,
            ResponseCode = entity.ResponseCode,
            ResponseBody = entity.ResponseBody,
            ErrorMessage = entity.ErrorMessage,
            DurationMs = entity.DurationMs,
            CreationTime = entity.CreationTime,
            CompletionTime = entity.CompletionTime,
        };
    }
}
