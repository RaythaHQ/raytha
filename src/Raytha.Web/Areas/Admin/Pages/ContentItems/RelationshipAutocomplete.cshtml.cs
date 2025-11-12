using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
public class RelationshipAutocomplete : BaseAdminPageModel
{
    public async Task<IActionResult> OnGet(
        [FromQuery] string relatedContentTypeId,
        [FromQuery] string q
    )
    {
        if (string.IsNullOrWhiteSpace(relatedContentTypeId))
        {
            return BadRequest(new { error = "relatedContentTypeId is required" });
        }

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return new JsonResult(new object[] { });
        }

        try
        {
            // Parse the content type ID
            if (!ShortGuid.TryParse(relatedContentTypeId, out ShortGuid contentTypeId))
            {
                return BadRequest(new { error = "Invalid relatedContentTypeId format" });
            }

            // Get the content type to retrieve its developer name
            var contentTypeResponse = await Mediator.Send(
                new GetContentTypeById.Query { Id = contentTypeId }
            );

            if (!contentTypeResponse.Success)
            {
                return BadRequest(new { error = "Content type not found" });
            }

            // Search for content items in the related content type
            var input = new GetContentItems.Query
            {
                ContentType = contentTypeResponse.Result.DeveloperName,
                Search = q,
                PageNumber = 1,
                PageSize = 10, // Limit autocomplete results
            };

            var response = await Mediator.Send(input);

            // Map results to autocomplete format
            var results = response
                .Result.Items.Select(item => new
                {
                    id = item.Id.ToString(),
                    label = item.PrimaryField,
                    value = item.PrimaryField,
                    key = item.Id.ToString(),
                })
                .ToList();

            return new JsonResult(results);
        }
        catch (Exception ex)
        {
            // Log the error (you might want to inject ILogger here)
            return StatusCode(500, new { error = "Error loading results", message = ex.Message });
        }
    }
}
