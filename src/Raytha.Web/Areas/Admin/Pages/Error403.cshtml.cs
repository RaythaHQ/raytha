using CSharpVitamins;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Web.Middlewares;

namespace Raytha.Web.Areas.Admin.Pages;

public class Error403Model : PageModel
{
    public string ErrorId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public bool IsDevelopmentMode { get; set; }

    public void OnGet()
    {
        var errorDetails = HttpContext.Items[ExceptionsMiddleware.ERROR_DETAILS_KEY] 
            as ExceptionsMiddleware.ErrorDetails;

        ErrorId = ShortGuid.NewGuid();
        ErrorMessage = errorDetails?.ErrorMessage ?? "Access denied";
        StackTrace = errorDetails?.StackTrace ?? string.Empty;
        IsDevelopmentMode = errorDetails?.IsDevelopmentMode ?? false;

        Response.StatusCode = 403;
    }
}

