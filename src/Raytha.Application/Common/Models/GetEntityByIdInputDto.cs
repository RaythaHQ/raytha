using CSharpVitamins;

namespace Raytha.Application.Common.Models;

public record GetEntityByIdInputDto
{
    public ShortGuid Id { get; init; }
}
