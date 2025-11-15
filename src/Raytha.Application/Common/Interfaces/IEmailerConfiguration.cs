namespace Raytha.Application.Common.Interfaces;

public interface IEmailerConfiguration
{
    public string SmtpHost { get; }

    public int SmtpPort { get; }

    public string SmtpUsername { get; }
    public string SmtpPassword { get; }

    public string SmtpFromAddress { get; }
    public string SmtpFromName { get; }
    public string SmtpDefaultFromName { get; }
    public string SmtpDefaultFromAddress { get; }

    public bool IsMissingSmtpEnvVars();
}
