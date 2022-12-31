namespace Raytha.Application.ContentTypes
{
    public record ContentTypeFieldChoiceInputDto
    {
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public bool Disabled { get; init; }
    }
}
