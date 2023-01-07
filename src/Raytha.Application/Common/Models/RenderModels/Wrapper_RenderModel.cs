using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentTypes;

namespace Raytha.Application.Common.Models.RenderModels;

public record Wrapper_RenderModel : IInsertTemplateVariable
{
    public ContentType_RenderModel ContentType { get; init; }
    public CurrentOrganization_RenderModel CurrentOrganization { get; init; }
    public CurrentUser_RenderModel CurrentUser { get; init; }
    public object Target { get; init; }
    public Dictionary<string, string> QueryParams { get; init; } = new Dictionary<string, string>();

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(QueryParams);
    }

    public virtual IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"{developerName}");
        }
    }

    public virtual string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
