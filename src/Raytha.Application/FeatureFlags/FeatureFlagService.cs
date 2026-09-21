using Microsoft.Extensions.Logging;
using Raytha.Domain.Entities;

namespace Raytha.Application.FeatureFlags;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagStore _store;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(IFeatureFlagStore store, ILogger<FeatureFlagService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default)
    {
        var definition = RaythaFeatureFlags.Find(key);
        if (definition is null)
        {
            return false;
        }

        try
        {
            var values = await _store.GetAllAsync(FeatureFlag.GlobalScope, cancellationToken);
            return values.TryGetValue(definition.Key, out var enabled)
                ? enabled
                : definition.DefaultEnabled;
        }
        catch (Exception ex)
        {
            // Fail closed: a broken store must never accidentally enable a feature.
            _logger.LogError(ex, "Feature flag store failed while evaluating {Key}", key);
            return false;
        }
    }

    public async Task<IReadOnlyList<ResolvedFeatureFlag>> ListAsync(
        CancellationToken cancellationToken = default
    )
    {
        var values = await _store.GetAllAsync(FeatureFlag.GlobalScope, cancellationToken);
        return RaythaFeatureFlags
            .All.Select(definition =>
                values.TryGetValue(definition.Key, out var enabled)
                    ? new ResolvedFeatureFlag(definition, enabled, "override")
                    : new ResolvedFeatureFlag(definition, definition.DefaultEnabled, "default")
            )
            .ToList();
    }

    public Task SetAsync(string key, bool enabled, CancellationToken cancellationToken = default)
    {
        var definition =
            RaythaFeatureFlags.Find(key)
            ?? throw new ArgumentException($"'{key}' is not a known feature flag.", nameof(key));

        return _store.SetAsync(FeatureFlag.GlobalScope, definition.Key, enabled, cancellationToken);
    }
}
