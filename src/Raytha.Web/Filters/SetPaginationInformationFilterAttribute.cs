using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Utils;
using System;
using System.Threading.Tasks;

namespace Raytha.Web.Filters;

public class SetPaginationInformationFilterAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();
        if (context.Controller is Controller controller)
        {
            string search = context.HttpContext.Request.Query["search"];
            int pageNumber = Convert.ToInt32(context.HttpContext.Request.Query["pageNumber"]);
            int pageSize = Convert.ToInt32(context.HttpContext.Request.Query["pageSize"]);
            pageSize = pageSize == 0 ? 50 : pageSize;
            string orderBy = context.HttpContext.Request.Query["orderBy"].ToString().Trim();
            string filter = context.HttpContext.Request.Query["filter"];
            string actionName = controller.RouteData.Values["Action"].ToString();

            var paginationModel = controller.ViewData.Model as IPagination_ViewModel;
            if (paginationModel == null)
                return;

            paginationModel.Search = search ?? string.Empty;
            paginationModel.Filter = filter ?? string.Empty;
            paginationModel.PageNumber = Math.Max(pageNumber, 1);
            paginationModel.PageSize = Math.Clamp(pageSize, 1, 1000);
            paginationModel.OrderByPropertyName = string.Empty;
            paginationModel.OrderByDirection = string.Empty;
            paginationModel.ActionName = actionName;

            if (string.IsNullOrEmpty(orderBy))
            {
                paginationModel.OrderByDirection = string.Empty;
                paginationModel.OrderByPropertyName = string.Empty;
            }
            else
            {
                var sortOrder = SplitOrderByPhrase.From(orderBy);
                if (sortOrder != null)
                {
                    paginationModel.OrderByPropertyName = sortOrder.PropertyName;
                    paginationModel.OrderByDirection = sortOrder.Direction;
                }
            }

            controller.ViewData.Model = paginationModel;
        }
    }
}
