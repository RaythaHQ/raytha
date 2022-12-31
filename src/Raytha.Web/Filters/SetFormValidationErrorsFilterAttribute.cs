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
        if (context.Controller is Controller controller)
        {
            await next();

            if (controller.ViewData.Model is IFormValidation validationModel)
            {
                if (controller.ViewData["ValidationErrors"] != null)
                {
                    validationModel.ValidationFailures = controller.ViewData["ValidationErrors"] as IEnumerable<ValidationFailure>;
                    controller.ViewData.Model = validationModel;
                }
            }
        }
    }
}
