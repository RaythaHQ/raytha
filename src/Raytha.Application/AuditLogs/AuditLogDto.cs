using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.AuditLogs;

public record AuditLogDto : BaseAuditableEntityDto
{
    public string Category { get; init; } = null!;
    public string Request { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public string IpAddress { get; set; } = null!;

    public ShortGuid? EntityId { get; init; }

    public static Expression<Func<AuditLog, AuditLogDto>> GetProjection()
    {
        return auditLog => GetProjection(auditLog);
    }
    public static AuditLogDto GetProjection(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            CreationTime = auditLog.CreationTime,
            Category = auditLog.Category,
            UserEmail = auditLog.UserEmail,
            Request = auditLog.Request,
            IpAddress = auditLog.IpAddress,
            EntityId = auditLog.EntityId
        };
    }
}
