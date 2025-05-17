using System.Text.Json;

namespace Raytha.Application.Common.Interfaces;

public interface IExecuteBackgroundTask
{
    Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}
