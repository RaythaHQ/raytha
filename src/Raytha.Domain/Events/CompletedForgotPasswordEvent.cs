namespace Raytha.Domain.Events;

public class CompletedForgotPasswordEvent : BaseEvent
{
    public User User { get; private set; }
    public bool SendEmail { get; private set; }
    public string NewPassword { get; private set; }
    public CompletedForgotPasswordEvent(User user, bool sendEmail, string newPassword)
    {
        User = user;
        SendEmail = sendEmail;
        NewPassword = newPassword;
    }
}
