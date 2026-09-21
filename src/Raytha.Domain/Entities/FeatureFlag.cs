namespace Raytha.Domain.Entities;

/// <summary>
/// A persisted override for a code-defined feature flag. Absence of a row means
/// the flag falls back to its code default; unknown keys always evaluate to false.
/// </summary>
public class FeatureFlag : BaseEntity, IHasCreationTime, IHasModificationTime
{
    public const string GlobalScope = "global";

    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    /// <summary>Optional scope for the override. Defaults to <see cref="GlobalScope"/>.</summary>
    public string Scope { get; set; } = GlobalScope;

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastModificationTime { get; set; }
}
