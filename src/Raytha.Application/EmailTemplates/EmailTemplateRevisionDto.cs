using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.EmailTemplates;

public record EmailTemplateRevisionDto : BaseAuditableEntityDto
{
    public string Content { get; init; } = string.Empty;
    public ShortGuid EmailTemplateId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Cc { get; init; }
    public string Bcc { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public EmailTemplateDto? EmailTemplate { get; init; }
    public static Expression<Func<EmailTemplateRevision, EmailTemplateRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static EmailTemplateRevisionDto GetProjection(EmailTemplateRevision entity)
    {
        if (entity == null)
            return null;

        return new EmailTemplateRevisionDto
        {
            Id = entity.Id,
            EmailTemplateId = entity.EmailTemplateId,
            Subject = entity.Subject,
            Content = entity.Content,
            Cc = entity.Cc,
            Bcc = entity.Bcc,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            EmailTemplate = EmailTemplateDto.GetProjection(entity.EmailTemplate)
        };
    }
}