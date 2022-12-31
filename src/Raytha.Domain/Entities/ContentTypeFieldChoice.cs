namespace Raytha.Domain.Entities;

public record ContentTypeFieldChoice
{
    public string? Label { get; init; }
    public string? DeveloperName { get; init; }
    public bool Disabled { get; init; }
}