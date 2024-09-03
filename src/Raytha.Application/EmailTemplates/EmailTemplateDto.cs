using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.EmailTemplates;

public record EmailTemplateDto : BaseAuditableEntityDto
{
    public string Subject { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public string Bcc { get; init; } = string.Empty;
    public string Cc { get; init; } = string.Empty;

    public static Expression<Func<EmailTemplate, EmailTemplateDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static EmailTemplateDto GetProjection(EmailTemplate entity)
    {
        if (entity == null)
            return null;

        return new EmailTemplateDto
        {
            Id = entity.Id,
            Subject = entity.Subject,
            DeveloperName = entity.DeveloperName,
            Content = entity.Content,
            Cc = entity.Cc,
            Bcc = entity.Bcc,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
        };
    }
}
