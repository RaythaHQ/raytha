namespace Raytha.Application.FeatureFlags;

public record ResolvedFeatureFlag(
    FeatureFlagDefinition Definition,
    bool IsEnabled,
    string Source // "default" or "override"
);

/// <summary>
/// Evaluates code-defined flags against persisted overrides. Unknown keys fail closed (false),
/// as does any store failure.
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResolvedFeatureFlag>> ListAsync(
        CancellationToken cancellationToken = default
    );

    Task SetAsync(string key, bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>Raw persistence of flag overrides. Implemented by the database.</summary>
public interface IFeatureFlagStore
{
    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(
        string scope,
        CancellationToken cancellationToken = default
    );

    Task SetAsync(
        string scope,
        string key,
        bool enabled,
        CancellationToken cancellationToken = default
    );
}
