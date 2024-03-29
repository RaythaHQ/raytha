using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;
using System.Net.Mail;
using System.Net;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.Services;

public class Emailer : IEmailer
{
    private readonly IEmailerConfiguration _configuration;

    public Emailer(IEmailerConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void SendEmail(EmailMessage message)
    {
        var smtpReplyToName = message.FromName.IfNullOrEmpty(_configuration.SmtpDefaultFromName);
        var smtpReplyToAddress = message.FromEmailAddress.IfNullOrEmpty(_configuration.SmtpDefaultFromAddress);

        using (var smtpClient = new SmtpClient(_configuration.SmtpHost)
        {
            Port = _configuration.SmtpPort,
            Credentials = new NetworkCredential(_configuration.SmtpUsername, _configuration.SmtpPassword),
            EnableSsl = _configuration.SmtpPort == 587 || _configuration.SmtpPort == 465,
        })
        {
            var messageToSend = new MailMessage();
            messageToSend.From = new MailAddress(_configuration.SmtpFromAddress, _configuration.SmtpFromName);
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
                messageToSend.Attachments.Add(new Attachment(new MemoryStream(attachment.Attachment), attachment.FileName));
            }

            messageToSend.Body = message.Content;
            messageToSend.IsBodyHtml = message.IsHtml;
            messageToSend.Subject = message.Subject;

            smtpClient.Send(messageToSend);
        }
    }
}
