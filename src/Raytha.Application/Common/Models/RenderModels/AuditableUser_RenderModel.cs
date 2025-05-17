using System.Linq.Expressions;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public record AuditableUser_RenderModel : IInsertTemplateVariable
{
    private string _prefix = string.Empty;

    private AuditableUser_RenderModel() { }

    private AuditableUser_RenderModel(string prefix)
    {
        _prefix = prefix;
    }

    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? EmailAddress { get; init; }
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }

    public static Expression<Func<AuditableUserDto, AuditableUser_RenderModel>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static AuditableUser_RenderModel GetProjection(AuditableUserDto entity)
    {
        if (entity == null)
            return null;

        return new AuditableUser_RenderModel
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            EmailAddress = entity.EmailAddress,
        };
    }

    public static AuditableUser_RenderModel FromPrefix(string prefix)
    {
        return new AuditableUser_RenderModel(prefix);
    }

    private IEnumerable<string> GetNakedDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(FirstName);
        yield return nameof(LastName);
        yield return nameof(EmailAddress);
        yield return nameof(FullName);
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in GetNakedDeveloperNames())
        {
            if (!string.IsNullOrEmpty(_prefix))
            {
                yield return $"{_prefix}.{developerName}";
            }
            else
            {
                yield return developerName;
            }
        }
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"{developerName}");
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
