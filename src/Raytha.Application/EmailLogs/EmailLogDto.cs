using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.EmailLogs;

public record EmailLogListItemDto : BaseEntityDto
{
    public string ToAddress { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public long DurationMs { get; init; }
    public DateTime CreationTime { get; init; }

    public static Expression<Func<EmailLog, EmailLogListItemDto>> GetProjection()
    {
        return entity => new EmailLogListItemDto
        {
            Id = entity.Id,
            ToAddress = entity.ToAddress,
            FromAddress = entity.FromAddress,
            Subject = entity.Subject,
            IsSuccess = entity.IsSuccess,
            ErrorMessage = entity.ErrorMessage,
            DurationMs = entity.DurationMs,
            CreationTime = entity.CreationTime,
        };
    }
}

public record EmailLogDto : EmailLogListItemDto
{
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; }

    public static EmailLogDto GetProjection(EmailLog entity)
    {
        return new EmailLogDto
        {
            Id = entity.Id,
            ToAddress = entity.ToAddress,
            FromAddress = entity.FromAddress,
            Subject = entity.Subject,
            Body = entity.Body,
            IsHtml = entity.IsHtml,
            IsSuccess = entity.IsSuccess,
            ErrorMessage = entity.ErrorMessage,
            DurationMs = entity.DurationMs,
            CreationTime = entity.CreationTime,
        };
    }
}
