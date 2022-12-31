using Raytha.Application.ContentTypes;

namespace Raytha.Application.Common.Models.RenderModels;

public record Wrapper_RenderModel
{
    public ContentType_RenderModel ContentType { get; init; }
    public CurrentOrganization_RenderModel CurrentOrganization { get; init; }
    public CurrentUser_RenderModel CurrentUser { get; init; }
    public object Target { get; init; }
}
