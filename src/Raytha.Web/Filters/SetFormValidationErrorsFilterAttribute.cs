using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Web.Areas.Admin.Views.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Raytha.Web.Filters;

public class SetFormValidationErrorsFilterAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (context.Controller is Controller controller)
        {
            if (controller.ViewData.Model is IFormValidation validationModel)
            {
                if (controller.ViewData["ValidationErrors"] != null)
                {
                    validationModel.ValidationFailures = controller.ViewData["ValidationErrors"] as Dictionary<string, string>;
                    controller.ViewData.Model = validationModel;
                }
            }
        }
    }
}
