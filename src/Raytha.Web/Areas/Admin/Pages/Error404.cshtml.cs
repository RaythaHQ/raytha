using CSharpVitamins;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Web.Middlewares;

namespace Raytha.Web.Areas.Admin.Pages;

public class Error404Model : PageModel
{
    public string ErrorId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public bool IsDevelopmentMode { get; set; }
    public string Route { get; set; } = string.Empty;

    public void OnGet()
    {
        // First try to get from Items (for non-redirected errors)
        var errorDetails = HttpContext.Items[ExceptionsMiddleware.ERROR_DETAILS_KEY] 
            as ExceptionsMiddleware.ErrorDetails;

        if (errorDetails != null)
        {
            ErrorMessage = errorDetails.ErrorMessage;
            StackTrace = errorDetails.StackTrace ?? string.Empty;
            IsDevelopmentMode = errorDetails.IsDevelopmentMode;
            Route = errorDetails.Route;
        }
        else
        {
            // Try to get from TempData (for redirected errors)
            var tempDataKey = ExceptionsMiddleware.ERROR_DETAILS_KEY;
            
            if (TempData.ContainsKey($"{tempDataKey}_Message"))
            {
                ErrorMessage = TempData[$"{tempDataKey}_Message"]?.ToString() ?? "The resource you requested was not found";
                StackTrace = TempData[$"{tempDataKey}_StackTrace"]?.ToString() ?? string.Empty;
                IsDevelopmentMode = (bool)(TempData[$"{tempDataKey}_IsDevelopment"] ?? false);
                Route = TempData[$"{tempDataKey}_Route"]?.ToString() ?? string.Empty;
            }
            else
            {
                ErrorMessage = "The resource you requested was not found";
            }
        }

        ErrorId = ShortGuid.NewGuid();
        Response.StatusCode = 404;
    }
}

