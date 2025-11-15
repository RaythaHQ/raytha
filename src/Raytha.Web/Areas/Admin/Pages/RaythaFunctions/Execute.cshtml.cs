using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[AllowAnonymous]
public class Execute : BaseAdminPageModel
{
    public async Task<IActionResult> OnGet(string developerName)
    {
        string payloadJson = null;
        if (HttpContext.Request.HasFormContentType)
        {
            payloadJson = System.Text.Json.JsonSerializer.Serialize(HttpContext.Request.Form);
        }
        else
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                payloadJson = await reader.ReadToEndAsync();
            }
        }
        var input = new ExecuteRaythaFunction.Command
        {
            DeveloperName = developerName,
            RequestMethod = HttpContext.Request.Method,
            QueryJson = JsonConvert.SerializeObject(HttpContext.Request.Query),
            PayloadJson = payloadJson,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            dynamic result = response.Result;
            return result.contentType switch
            {
                "application/json" => new JsonResult(
                    result.body,
                    new JsonSerializerOptions { PropertyNamingPolicy = null, WriteIndented = true }
                ),
                "text/html" => Content(result.body, result.contentType),
                "redirectToUrl" => Redirect(result.body),
                "statusCode" => StatusCode(result.statusCode, result.body),
                _ => throw new NotSupportedException(),
            };
        }
        else
        {
            var statusCode = response.GetErrors().First().PropertyName switch
            {
                "IsActive" => StatusCodes.Status404NotFound,
                "Queue of functions" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            };

            return StatusCode(statusCode, response.Error);
        }
    }
}
