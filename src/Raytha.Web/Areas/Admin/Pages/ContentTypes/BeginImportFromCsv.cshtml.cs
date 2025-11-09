using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class BeginImportFromCsv : BaseHasFavoriteViewsPageModel
{
    [BindProperty]
    public string ImportMethod { get; set; } = "add_new_records_only";

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    [BindProperty]
    public bool ImportAsDraft { get; set; }

    public string? TaskId { get; set; }
    public string? PathBase { get; set; }
    public string BackToListUrl { get; set; } = string.Empty;

    public async Task<IActionResult> OnGet(
        string contentTypeDeveloperName,
        string taskId = null,
        string backToListUrl = ""
    )
    {
        TaskId = taskId;
        BackToListUrl = backToListUrl;
        PathBase = CurrentOrganization.PathBase;
        return Page();
    }

    public async Task<IActionResult> OnPost(
        string contentTypeDeveloperName,
        string backToListUrl = ""
    )
    {
        byte[]? fileBytes = null;

        if (ImportFile != null)
        {
            using var stream = ImportFile.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        var input = new BeginImportContentItemsFromCsv.Command
        {
            ContentTypeId = CurrentView.ContentTypeId,
            ImportMethod = ImportMethod,
            ImportAsDraft = ImportAsDraft,
            CsvAsBytes = fileBytes,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Import in progress.");
            return RedirectToPage(
                "/ContentTypes/BeginImportFromCsv",
                new
                {
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                    taskId = response.Result,
                    backToListUrl = backToListUrl,
                }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting while importing. See the error below.",
                response.GetErrors()
            );
            BackToListUrl = backToListUrl;
            return Page();
        }
    }

    public async Task<IActionResult> OnGetStatus(string id)
    {
        var response = await Mediator.Send(new GetBackgroundTaskById.Query { Id = id });
        return new JsonResult(response.Result);
    }
}
