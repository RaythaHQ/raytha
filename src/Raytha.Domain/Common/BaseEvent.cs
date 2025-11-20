using Mediator;

namespace Raytha.Domain.Common;

public abstract class BaseEvent : INotification { }

public interface IBeforeSaveChangesNotification : INotification { }

public interface IAfterSaveChangesNotification : INotification { }
