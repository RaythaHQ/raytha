namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionConfiguration
{
    public bool IsEnabled { get; }
    public int MaxActive { get; }
    public TimeSpan ExecuteTimeout { get; }
    public TimeSpan QueueTimeout { get; }
}
