using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public abstract record BaseFormSubmit_RenderModel : IInsertTemplateVariable
{
    public string? SuccessMessage { get; set; }
    public string? RequestVerificationToken { get; set; }
    public Dictionary<string, string>? ValidationFailures { get; set; }

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(RequestVerificationToken);
        yield return nameof(ValidationFailures);
        yield return nameof(SuccessMessage);
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