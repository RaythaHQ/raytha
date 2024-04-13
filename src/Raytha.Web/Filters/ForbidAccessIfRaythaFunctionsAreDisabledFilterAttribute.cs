using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Common.Interfaces;
using System.Threading.Tasks;

namespace Raytha.Web.Filters;

public class ForbidAccessIfRaythaFunctionsAreDisabledFilterAttribute : ActionFilterAttribute
{
    private IRaythaFunctionConfiguration _configuration;

    public ForbidAccessIfRaythaFunctionsAreDisabledFilterAttribute(IRaythaFunctionConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_configuration.IsEnabled)
        {
            context.Result = new ContentResult
            {
                Content = "Functions are disabled",
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
        else
        {
            await next();
        }
    }
}