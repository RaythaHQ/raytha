namespace Raytha.Domain.Events;

public class AdminPasswordChangedEvent : BaseEvent, IBeforeSaveChangesNotification
{
    public User User { get; private set; }
    public bool SendEmail { get; private set; }

    public AdminPasswordChangedEvent(User user, bool sendEmail)
    {
        User = user;
        SendEmail = sendEmail;
    }
}
