using CSharpVitamins;

namespace Raytha.Application.Common.Models;

public interface IBaseEntityDto
{
    ShortGuid Id { get; init; }
}

public record BaseEntityDto : IBaseEntityDto
{
    public ShortGuid Id { get; init; }
}
