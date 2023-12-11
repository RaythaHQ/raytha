using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.Services;

public class Emailer : IEmailer
{
    private readonly IRaythaDbContext _db;
    private readonly IConfiguration _configuration;
    public Emailer(IRaythaDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public void SendEmail(EmailMessage message)
    {
        var entity = _db.OrganizationSettings.First();

        string smtpHost;
        int smtpPort;
        string smtpPassword;
        string smtpUsername;
        string smtpFromName;
        string smtpFromAddress;
        string smtpReplyToName;
        string smtpReplyToAddress;
        if (entity.SmtpOverrideSystem)
        {
            smtpHost = entity.SmtpHost;
            smtpPort = entity.SmtpPort.HasValue ? entity.SmtpPort.Value : 25;
            smtpUsername = entity.SmtpUsername ?? string.Empty;
            smtpPassword = entity.SmtpPassword ?? string.Empty;
        }
        else
        {
            smtpHost = _configuration["SMTP_HOST"];
            smtpPort = Convert.ToInt32(_configuration["SMTP_PORT"]);
            smtpUsername = _configuration["SMTP_USERNAME"];
            smtpPassword = _configuration["SMTP_PASSWORD"];
        }

        smtpFromAddress = _configuration["SMTP_FROM_ADDRESS"].IfNullOrEmpty(entity.SmtpDefaultFromAddress);
        smtpFromName = _configuration["SMTP_FROM_NAME"].IfNullOrEmpty(entity.SmtpDefaultFromName);
        smtpReplyToName = message.FromName.IfNullOrEmpty(entity.SmtpDefaultFromName);
        smtpReplyToAddress = message.FromEmailAddress.IfNullOrEmpty(entity.SmtpDefaultFromAddress);

        using (var smtpClient = new SmtpClient(smtpHost)
        {
            Port = smtpPort,
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = smtpPort == 587 || smtpPort == 465,
        })
        {
            var messageToSend = new MailMessage();
            messageToSend.From = new MailAddress(smtpFromAddress, smtpFromName);
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

    public bool IsMissingSmtpEnvVars()
    {
        return string.IsNullOrEmpty(_configuration["SMTP_HOST"]);
    }
}
