using Raytha.Domain.Common;

namespace Raytha.Application.Common.Interfaces;

public interface IEmailer
{
    void SendEmail(EmailMessage message);
    bool IsMissingSmtpEnvVars();
}
