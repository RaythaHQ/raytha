using Raytha.Application.Common.Models.RenderModels;

namespace Raytha.Application.Admins;

public record SendAdminWelcomeEmail_RenderModel : BaseSendWelcomeEmail_RenderModel
{
    public bool IsNewlyCreatedUser { get; init; }

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(IsNewlyCreatedUser);
    }
}

public record SendAdminPasswordChanged_RenderModel : BaseSendPasswordChanged_RenderModel { }

public record SendAdminPasswordReset_RenderModel : BaseSendPasswordReset_RenderModel { }
