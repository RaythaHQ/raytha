using Microsoft.ClearScript.V8;

namespace Raytha.Infrastructure.RaythaFunctions;

public interface IV8EnginePool : IDisposable
{
    V8ScriptEngine Rent();
    void Return(V8ScriptEngine engine);
}

