using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionSemaphore : SemaphoreSlim, IRaythaFunctionSemaphore
{
    public RaythaFunctionSemaphore(IRaythaFunctionConfiguration configuration) : base(configuration.MaxActive)
    {
    }
}