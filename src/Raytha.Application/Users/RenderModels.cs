using Raytha.Application.Common.Models.RenderModels;

namespace Raytha.Application.Users;

public record SendUserWelcomeEmail_RenderModel : BaseSendWelcomeEmail_RenderModel { }

public record SendUserPasswordChanged_RenderModel : BaseSendPasswordChanged_RenderModel { }

public record SendUserPasswordReset_RenderModel : BaseSendPasswordReset_RenderModel { }

public record ChangeProfileSubmit_RenderModel : BaseFormSubmit_RenderModel { }

public record ChangePasswordSubmit_RenderModel : BaseFormSubmit_RenderModel { }
