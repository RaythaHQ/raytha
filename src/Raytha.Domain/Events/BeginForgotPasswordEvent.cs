namespace Raytha.Domain.Events;

public class BeginForgotPasswordEvent : BaseEvent, IBeforeSaveChangesNotification
{
    public User User { get; private set; }
    public bool SendEmail { get; private set; }
    public string Token { get; private set; }
    public BeginForgotPasswordEvent(User user, bool sendEmail, string token)
    {
        User = user;
        SendEmail = sendEmail;
        Token = token;
    }
}
