namespace Raytha.Domain.Common;

public record EmailMessage
{
    public Guid RequestId { get; } = Guid.NewGuid();

    public string Subject { get; init; }
    public string Content { get; init; }
    public bool IsHtml { get; init; } = true;
    public string FromEmailAddress { get; init; }
    public string FromName { get; init; }
    public IEnumerable<string> To { get; init; } = new List<string>();
    public IEnumerable<string> Cc { get; init; } = new List<string>();
    public IEnumerable<string> Bcc { get; init; } = new List<string>();
    public IEnumerable<EmailMessageAttachment> Attachments { get; init; } =
        new List<EmailMessageAttachment>();

    public EmailMessage() { }

    private EmailMessage(
        string subject,
        string content,
        string to,
        string fromEmailAddress,
        string fromName
    )
    {
        Subject = subject;
        Content = content;
        IsHtml = true;
        To = new List<string> { to };
        FromName = fromName;
        FromEmailAddress = fromEmailAddress;
    }

    public static EmailMessage From(
        string subject,
        string content,
        string to,
        string fromEmailAddress,
        string fromName
    )
    {
        return new EmailMessage(subject, content, to, fromEmailAddress, fromName);
    }
}

public record EmailMessageAttachment
{
    public byte[] Attachment { get; init; }
    public string FileName { get; init; }
}
