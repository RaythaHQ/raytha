using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public record GenericError_RenderModel : IInsertTemplateVariable
{
    public string ErrorId { get; init; }
    public string ErrorMessage { get; init; }

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(ErrorId);
        yield return nameof(ErrorMessage);
    }

    public virtual IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"Target.{developerName}");
        }
    }

    public virtual string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}