using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Interfaces;

public interface IBackgroundTaskDb
{
    BackgroundTask DequeueBackgroundTask();
}
