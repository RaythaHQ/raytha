using System.Text;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.ContentTypes;

public record ContentType_RenderModel : IInsertTemplateVariable
{
    public string Id { get; init; }
    public string LabelPlural { get; init; }
    public string LabelSingular { get; init; }
    public string DeveloperName { get; init; }
    public string Description { get; init; }

    public static ContentType_RenderModel GetProjection(ContentTypeDto entity)
    {
        if (entity == null)
            return null;

        return new ContentType_RenderModel
        {
            Id = entity.Id,
            LabelPlural = entity.LabelPlural,
            LabelSingular = entity.LabelSingular,
            DeveloperName = entity.DeveloperName,
            Description = entity.Description,
        };
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(LabelPlural);
        yield return nameof(LabelSingular);
        yield return nameof(DeveloperName);
        yield return nameof(Description);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(
                developerName,
                $"ContentType.{developerName}"
            );
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
