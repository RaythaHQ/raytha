namespace Raytha.Application.FeatureFlags;

public record FeatureFlagDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool DefaultEnabled { get; init; }
    public bool IsEnabled { get; init; }
    public string Source { get; init; } = "default";

    public static FeatureFlagDto FromResolved(ResolvedFeatureFlag resolved)
    {
        return new FeatureFlagDto
        {
            Key = resolved.Definition.Key,
            Label = resolved.Definition.Label,
            Description = resolved.Definition.Description,
            DefaultEnabled = resolved.Definition.DefaultEnabled,
            IsEnabled = resolved.IsEnabled,
            Source = resolved.Source,
        };
    }
}
