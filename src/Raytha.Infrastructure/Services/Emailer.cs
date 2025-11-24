using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Common;

namespace Raytha.Infrastructure.Services;

public class Emailer : IEmailer
{
    private readonly IEmailerConfiguration _configuration;
    private readonly ILogger<Emailer> _logger;

    public Emailer(IEmailerConfiguration configuration, ILogger<Emailer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void SendEmail(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(_configuration.SmtpHost))
        {
            _logger.LogWarning("SMTP host is not configured, skipping email send");
            return;
        }
        var smtpReplyToName = message.FromName.IfNullOrEmpty(_configuration.SmtpDefaultFromName);
        var smtpReplyToAddress = message.FromEmailAddress.IfNullOrEmpty(
            _configuration.SmtpDefaultFromAddress
        );

        using (
            var smtpClient = new SmtpClient(_configuration.SmtpHost)
            {
                Port = _configuration.SmtpPort,
                Credentials = new NetworkCredential(
                    _configuration.SmtpUsername,
                    _configuration.SmtpPassword
                ),
                EnableSsl = _configuration.SmtpPort == 587 || _configuration.SmtpPort == 465,
            }
        )
        {
            var messageToSend = new MailMessage();
            messageToSend.From = new MailAddress(
                _configuration.SmtpFromAddress,
                _configuration.SmtpFromName
            );
            messageToSend.ReplyToList.Add(new MailAddress(smtpReplyToAddress, smtpReplyToName));

            foreach (var to in message.To)
            {
                messageToSend.To.Add(to);
            }

            foreach (var cc in message.Cc)
            {
                messageToSend.CC.Add(cc);
            }

            foreach (var bcc in message.Bcc)
            {
                messageToSend.Bcc.Add(bcc);
            }

            foreach (var attachment in message.Attachments)
            {
                messageToSend.Attachments.Add(
                    new Attachment(new MemoryStream(attachment.Attachment), attachment.FileName)
                );
            }

            messageToSend.Body = message.Content;
            messageToSend.IsBodyHtml = message.IsHtml;
            messageToSend.Subject = message.Subject;

            smtpClient.Send(messageToSend);
        }
    }
}
