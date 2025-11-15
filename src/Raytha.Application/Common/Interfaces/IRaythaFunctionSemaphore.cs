namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionSemaphore
{
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
    int Release();
}
