namespace Raytha.Domain.Events;

public class BeginLoginWithMagicLinkEvent : BaseEvent
{
    public User User { get; private set; }
    public bool SendEmail { get; private set; }
    public string Token { get; private set; }
    public string ReturnUrl { get; private set; }
    public int MagicLinkExpiresInSeconds { get; private set; }  
    public BeginLoginWithMagicLinkEvent(User user, bool sendEmail, string token, string returnUrl, int magicLinkExpiresInSeconds)
    {
        User = user;
        SendEmail = sendEmail;
        Token = token;
        ReturnUrl = returnUrl;
        MagicLinkExpiresInSeconds = magicLinkExpiresInSeconds;
    }
}
