namespace Raytha.Application.Common.Interfaces;

public interface IInsertTemplateVariable
{
    public IEnumerable<string> GetDeveloperNames();
    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables();
    public string GetTemplateVariablesAsForEachLiquidSyntax();
}
