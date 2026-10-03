using System.Net;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Common;

namespace Raytha.Application.Maintenance.Commands;

/// <summary>
/// Sends one plain test message through the configured emailer so an operator can prove SMTP
/// works. The send goes through the same <see cref="IEmailer"/> as every other outbound email,
/// so it is recorded in the email log and a failure is the real SMTP failure.
/// </summary>
public class SendTestEmail
{
    public record Command : LoggableRequest<CommandResponseDto<SentTestEmailDto>>
    {
        public string EmailAddress { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EmailAddress)
                .NotEmpty()
                .WithMessage("Recipient email address is required.")
                .Must(address => address != null && address.Trim().IsValidEmailAddress())
                .WithMessage("Recipient must be a valid email address.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<SentTestEmailDto>>
    {
        private readonly IEmailer _emailer;
        private readonly IEmailerConfiguration _emailerConfiguration;
        private readonly ICurrentOrganization _currentOrganization;

        public Handler(
            IEmailer emailer,
            IEmailerConfiguration emailerConfiguration,
            ICurrentOrganization currentOrganization
        )
        {
            _emailer = emailer;
            _emailerConfiguration = emailerConfiguration;
            _currentOrganization = currentOrganization;
        }

        public ValueTask<CommandResponseDto<SentTestEmailDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // The emailer skips silently without a host; a test must say so instead of looking sent.
            if (string.IsNullOrWhiteSpace(_emailerConfiguration.SmtpHost))
                return ValueTask.FromResult(
                    new CommandResponseDto<SentTestEmailDto>(
                        "EmailAddress",
                        "SMTP is not configured. Set the SMTP host in settings or the environment before sending."
                    )
                );

            var to = request.EmailAddress.Trim();
            var organization = WebUtility.HtmlEncode(_currentOrganization.OrganizationName);
            var sentAt = DateTime.UtcNow;
            var message = new EmailMessage
            {
                To = new List<string> { to },
                Subject = $"Test email from {_currentOrganization.OrganizationName}",
                Content =
                    $"<p>This is a test email from <strong>{organization}</strong>.</p>"
                    + $"<p>It was sent from the Maintenance page at {sentAt:yyyy-MM-dd HH:mm:ss} UTC "
                    + $"through SMTP host <code>{WebUtility.HtmlEncode(_emailerConfiguration.SmtpHost)}</code>.</p>"
                    + "<p>If you are reading this, outbound email is working.</p>",
            };

            try
            {
                _emailer.SendEmail(message);
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(
                    new CommandResponseDto<SentTestEmailDto>(
                        "EmailAddress",
                        $"SMTP rejected the message: {ex.Message}"
                    )
                );
            }

            return ValueTask.FromResult(
                new CommandResponseDto<SentTestEmailDto>(new SentTestEmailDto(to, sentAt))
            );
        }
    }
}

public record SentTestEmailDto(string EmailAddress, DateTime SentAt);
