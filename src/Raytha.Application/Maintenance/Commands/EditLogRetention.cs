using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using OrganizationSettingsEntity = Raytha.Domain.Entities.OrganizationSettings;

namespace Raytha.Application.Maintenance.Commands;

public class EditLogRetention
{
    public record Command : LoggableRequest<CommandResponseDto<LogRetentionDto>>
    {
        public int AuditLogRetentionDays { get; init; }
        public int EmailLogRetentionDays { get; init; }
        public int WebhookDeliveryRetentionDays { get; init; }
        public int BackgroundTaskRetentionDays { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AuditLogRetentionDays).Must(BeAValidWindow).WithMessage(Message);
            RuleFor(x => x.EmailLogRetentionDays).Must(BeAValidWindow).WithMessage(Message);
            RuleFor(x => x.WebhookDeliveryRetentionDays).Must(BeAValidWindow).WithMessage(Message);
            RuleFor(x => x.BackgroundTaskRetentionDays).Must(BeAValidWindow).WithMessage(Message);
        }

        private const string Message =
            "Retention must be between 0 and 3650 days. Use 0 to keep entries forever.";

        private static bool BeAValidWindow(int days) =>
            days is >= 0 and <= OrganizationSettingsEntity.MAX_RETENTION_DAYS;
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<LogRetentionDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<LogRetentionDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.OrganizationSettings.FirstAsync(cancellationToken);

            entity.AuditLogRetentionDays = request.AuditLogRetentionDays;
            entity.EmailLogRetentionDays = request.EmailLogRetentionDays;
            entity.WebhookDeliveryRetentionDays = request.WebhookDeliveryRetentionDays;
            entity.BackgroundTaskRetentionDays = request.BackgroundTaskRetentionDays;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<LogRetentionDto>(LogRetentionDto.GetProjection(entity));
        }
    }
}
