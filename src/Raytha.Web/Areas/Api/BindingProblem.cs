using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Raytha.Web.Areas.Api;

/// <summary>
/// The 400 an [ApiController] answers when the request does not bind.
/// </summary>
public static class BindingProblem
{
    public static ValidationProblemDetails Create(ModelStateDictionary modelState, string? instance)
    {
        return new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Instance = instance,
        };
    }
}
