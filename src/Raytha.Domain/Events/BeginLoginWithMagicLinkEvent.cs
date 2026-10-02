namespace Raytha.Domain.Events;

public class BeginLoginWithMagicLinkEvent : BaseEvent, IBeforeSaveChangesNotification
{
    public User User { get; private set; }
    public bool SendEmail { get; private set; }
    public string Code { get; private set; }
    public int MagicLinkExpiresInSeconds { get; private set; }

    public BeginLoginWithMagicLinkEvent(
        User user,
        bool sendEmail,
        string code,
        int magicLinkExpiresInSeconds
    )
    {
        User = user;
        SendEmail = sendEmail;
        Code = code;
        MagicLinkExpiresInSeconds = magicLinkExpiresInSeconds;
    }
}
