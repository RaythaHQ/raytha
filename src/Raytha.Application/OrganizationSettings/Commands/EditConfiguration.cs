using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.OrganizationSettings.Commands;

public class EditConfiguration
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string OrganizationName { get; init; } = null!;
        public string WebsiteUrl { get; init; } = null!;
        public string TimeZone { get; init; } = null!;
        public string DateFormat { get; init; } = null!;
        public string SmtpDefaultFromAddress { get; init; } = null!;
        public string SmtpDefaultFromName { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrganizationName).NotEmpty();
            RuleFor(x => x.TimeZone)
                .Must(DateTimeExtensions.IsValidTimeZone)
                .WithMessage(p => $"{p.TimeZone} timezone is unrecognized.");
            RuleFor(x => x.DateFormat)
                .Must(DateTimeExtensions.IsValidDateFormat)
                .WithMessage(p => $"{p.DateFormat} format is unrecognized.");
            RuleFor(x => x.WebsiteUrl)
                .Must(StringExtensions.IsValidUriFormat)
                .WithMessage(p => $"{p.WebsiteUrl} must be a valid URI format.");
            RuleFor(x => x.SmtpDefaultFromAddress).EmailAddress();
            RuleFor(x => x.SmtpDefaultFromName).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.OrganizationSettings.First();

            entity.OrganizationName = request.OrganizationName;
            entity.WebsiteUrl = request.WebsiteUrl;
            entity.TimeZone = request.TimeZone;
            entity.DateFormat = request.DateFormat;
            entity.SmtpDefaultFromAddress = request.SmtpDefaultFromAddress;
            entity.SmtpDefaultFromName = request.SmtpDefaultFromName;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
