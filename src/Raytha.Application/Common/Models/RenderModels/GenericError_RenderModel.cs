using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public record GenericError_RenderModel : IInsertTemplateVariable
{
    public string ErrorId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string StackTrace { get; init; } = string.Empty;
    public bool IsDevelopmentMode { get; init; }

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(ErrorId);
        yield return nameof(ErrorMessage);
        yield return nameof(StackTrace);
        yield return nameof(IsDevelopmentMode);
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
