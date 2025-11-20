#nullable enable
using FluentValidation.Results;
using Mediator;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Web.Areas.Shared.Models;

/// <summary>
/// Base class for all Razor Pages in the application.
/// Provides common services, error handling, and pagination support via service locator pattern.
/// </summary>
public abstract class BasePageModel : PageModel
{
    /// <summary>
    /// TempData key for error messages.
    /// </summary>
    public const string ErrorMessageKey = "ErrorMessage";

    /// <summary>
    /// TempData key for success messages.
    /// </summary>
    public const string SuccessMessageKey = "SuccessMessage";

    /// <summary>
    /// TempData key for warning messages.
    /// </summary>
    public const string WarningMessageKey = "WarningMessage";

    private ISender? _mediator;
    private ICurrentOrganization? _currentOrganization;
    private ICurrentUser? _currentUser;
    private IFileStorageProviderSettings? _fileStorageSettings;
    private IFileStorageProvider? _fileStorageProvider;
    private IEmailer? _emailer;
    private IEmailerConfiguration? _emailerConfiguration;
    private IRelativeUrlBuilder? _relativeUrlBuilder;
    private ILogger? _logger;

    /// <summary>
    /// Gets the MediatR sender for executing commands and queries.
    /// </summary>
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Gets the current organization context.
    /// </summary>
    protected ICurrentOrganization CurrentOrganization =>
        _currentOrganization ??=
            HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();

    /// <summary>
    /// Gets the current user context.
    /// </summary>
    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>
    /// Gets the file storage provider for uploading and retrieving files.
    /// </summary>
    protected IFileStorageProvider FileStorageProvider =>
        _fileStorageProvider ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProvider>();

    /// <summary>
    /// Gets the file storage provider settings.
    /// </summary>
    protected IFileStorageProviderSettings FileStorageProviderSettings =>
        _fileStorageSettings ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProviderSettings>();

    /// <summary>
    /// Gets the emailer service for sending emails.
    /// </summary>
    protected IEmailer Emailer =>
        _emailer ??= HttpContext.RequestServices.GetRequiredService<IEmailer>();

    /// <summary>
    /// Gets the emailer configuration.
    /// </summary>
    protected IEmailerConfiguration EmailerConfiguration =>
        _emailerConfiguration ??=
            HttpContext.RequestServices.GetRequiredService<IEmailerConfiguration>();

    /// <summary>
    /// Gets the relative URL builder for constructing application URLs.
    /// </summary>
    protected IRelativeUrlBuilder RelativeUrlBuilder =>
        _relativeUrlBuilder ??=
            HttpContext.RequestServices.GetRequiredService<IRelativeUrlBuilder>();

    /// <summary>
    /// Gets the logger for this page model. Uses generic type based on the derived class.
    /// </summary>
    protected ILogger Logger =>
        _logger ??= HttpContext
            .RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType());

    /// <summary>
    /// Gets or sets the dictionary of validation failures keyed by property name.
    /// </summary>
    public Dictionary<string, string>? ValidationFailures { get; set; }

    /// <summary>
    /// Called before the page handler method executes. Handles redirects, setup checks, and pagination setup.
    /// </summary>
    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next
    )
    {
        if (CurrentOrganization.RedirectWebsite.IsValidUriFormat())
        {
            Logger.LogInformation(
                "Redirecting to configured redirect website: {RedirectWebsite}",
                CurrentOrganization.RedirectWebsite
            );
            context.HttpContext.Response.Redirect(CurrentOrganization.RedirectWebsite);
            return;
        }

        string? currentPage = RouteData.Values["page"]?.ToString();
        if (
            !CurrentOrganization.InitialSetupComplete
            && !string.IsNullOrEmpty(currentPage)
            && currentPage != "/Setup/Index"
        )
        {
            Logger.LogInformation(
                "Initial setup not complete. Redirecting to setup page from {CurrentPage}",
                currentPage
            );
            context.HttpContext.Response.Redirect($"{CurrentOrganization.PathBase}/raytha/setup");
            return;
        }

        await next();
        CheckIfErrorOrSuccessMessageExist();

        var handlerInstance = context.HandlerInstance;
        var instanceType = handlerInstance.GetType();
        var hasListViewInterface = instanceType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasListView<>)
            );

        if (hasListViewInterface != null)
        {
            string search = context.HttpContext.Request.Query["search"].ToString();

            // Security: Use safe parsing and sane defaults for paging parameters so that malformed
            // or malicious query values cannot trigger FormatException-based error paths or force
            // extreme page sizes; this preserves existing behavior for valid inputs.
            var pageNumberRaw = context.HttpContext.Request.Query["pageNumber"].ToString();
            var pageSizeRaw = context.HttpContext.Request.Query["pageSize"].ToString();

            int pageNumber = 1;
            if (!string.IsNullOrWhiteSpace(pageNumberRaw))
            {
                int.TryParse(pageNumberRaw, out pageNumber);
            }

            int pageSize = 50;
            if (
                !string.IsNullOrWhiteSpace(pageSizeRaw)
                && int.TryParse(pageSizeRaw, out var parsedPageSize)
            )
            {
                pageSize = parsedPageSize;
            }

            string orderBy = context.HttpContext.Request.Query["orderBy"].ToString().Trim();
            string filter = context.HttpContext.Request.Query["filter"].ToString();
            string? actionName = context.RouteData.Values["page"]?.ToString();

            var listViewProperty = hasListViewInterface.GetProperty("ListView");
            if (listViewProperty != null)
            {
                var listViewValue = listViewProperty.GetValue(handlerInstance);
                if (listViewValue is IPaginationViewModel paginationModel)
                {
                    paginationModel.Search = search ?? string.Empty;
                    paginationModel.Filter = filter ?? string.Empty;
                    paginationModel.PageNumber = Math.Max(pageNumber, 1);
                    paginationModel.PageSize = Math.Clamp(pageSize, 1, 1000);
                    paginationModel.OrderByPropertyName = string.Empty;
                    paginationModel.OrderByDirection = string.Empty;
                    paginationModel.PageName = actionName ?? string.Empty;

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

                    listViewProperty.SetValue(handlerInstance, paginationModel);
                }
            }
        }
    }

    /// <summary>
    /// Checks if error or success messages exist in TempData and copies them to ViewData.
    /// </summary>
    protected void CheckIfErrorOrSuccessMessageExist()
    {
        if (TempData[ErrorMessageKey] != null)
        {
            ViewData[ErrorMessageKey] = TempData[ErrorMessageKey];
        }

        if (TempData[SuccessMessageKey] != null)
        {
            ViewData[SuccessMessageKey] = TempData[SuccessMessageKey];
        }
    }

    /// <summary>
    /// Sets an error message to be displayed to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected void SetErrorMessage(string message)
    {
        ViewData[ErrorMessageKey] = message;
        TempData[ErrorMessageKey] = message;
        Logger.LogWarning("Page error message set: {ErrorMessage}", message);
    }

    /// <summary>
    /// Sets error messages from validation failures.
    /// </summary>
    /// <param name="errors">The collection of validation failures.</param>
    /// <param name="statusCode">The HTTP status code to set (default 303).</param>
    protected void SetErrorMessage(IEnumerable<ValidationFailure>? errors, int statusCode = 303)
    {
        SetErrorMessage(string.Empty, errors, statusCode);
    }

    /// <summary>
    /// Sets error messages from validation failures with an optional custom message.
    /// </summary>
    /// <param name="message">The custom error message (optional).</param>
    /// <param name="errors">The collection of validation failures.</param>
    /// <param name="statusCode">The HTTP status code to set (default 303).</param>
    protected void SetErrorMessage(
        string message,
        IEnumerable<ValidationFailure>? errors,
        int statusCode = 303
    )
    {
        var validationSummary = errors?.FirstOrDefault(p =>
            p.PropertyName == Constants.VALIDATION_SUMMARY
        );
        if (validationSummary != null)
        {
            SetErrorMessage(validationSummary.ErrorMessage);
        }
        else if (!string.IsNullOrEmpty(message))
        {
            SetErrorMessage(message);
        }
        else
        {
            SetErrorMessage("There was an error processing your request.");
        }

        ValidationFailures = errors?.ToDictionary(
            k => k.PropertyName,
            v => v.ErrorMessage ?? string.Empty
        );
        HttpContext.Response.StatusCode = statusCode;

        if (errors != null && errors.Any())
        {
            Logger.LogWarning(
                "Validation failures occurred: {ValidationErrors}",
                string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
            );
        }
    }

    /// <summary>
    /// Sets a success message to be displayed to the user.
    /// </summary>
    /// <param name="message">The success message to display.</param>
    protected void SetSuccessMessage(string message)
    {
        ViewData[SuccessMessageKey] = message;
        TempData[SuccessMessageKey] = message;
        Logger.LogInformation("Success message set: {SuccessMessage}", message);
    }

    /// <summary>
    /// Sets a warning message to be displayed to the user.
    /// </summary>
    /// <param name="message">The warning message to display.</param>
    protected void SetWarningMessage(string message)
    {
        ViewData[WarningMessageKey] = message;
        TempData[WarningMessageKey] = message;
        Logger.LogWarning("Warning message set: {WarningMessage}", message);
    }

    /// <summary>
    /// Returns "is-invalid" CSS class if the specified property has validation errors.
    /// </summary>
    /// <param name="propertyName">The property name to check for errors.</param>
    /// <returns>"is-invalid" or empty string.</returns>
    public string HasError(string propertyName)
    {
        return ValidationFailures != null && ValidationFailures.ContainsKey(propertyName)
            ? "is-invalid"
            : string.Empty;
    }

    /// <summary>
    /// Returns the error message for the specified property if validation errors exist.
    /// </summary>
    /// <param name="propertyName">The property name to retrieve error message for.</param>
    /// <returns>The error message or null if none exists.</returns>
    public string? ErrorMessageFor(string propertyName)
    {
        return ValidationFailures?.GetValueOrDefault(propertyName);
    }

    /// <summary>
    /// Sets breadcrumbs for the current page.
    /// Breadcrumbs will be rendered by the breadcrumbs TagHelper in the layout.
    /// </summary>
    /// <param name="breadcrumbs">The breadcrumb nodes to display.</param>
    protected void SetBreadcrumbs(params BreadcrumbNode[] breadcrumbs)
    {
        ViewData["Breadcrumbs"] = breadcrumbs;
    }
}

/// <summary>
/// Interface for page models that display paginated list views.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public interface IHasListView<T>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated items.
    /// </summary>
    public ListViewModel<T> ListView { get; set; }
}

/// <summary>
/// Helper class to split an orderBy query string into property name and direction.
/// </summary>
public class SplitOrderByPhrase
{
    /// <summary>
    /// Gets or sets the property name to sort by.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction (asc or desc).
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Parses an orderBy string into property name and direction.
    /// </summary>
    /// <param name="orderByPhrase">The orderBy phrase (e.g., "Name asc").</param>
    /// <returns>A SplitOrderByPhrase object or null if parsing fails.</returns>
    public static SplitOrderByPhrase? From(string orderByPhrase)
    {
        try
        {
            var firstSortOrderItem = orderByPhrase
                .Split(",")
                .Select(p => p.Trim())
                .ToArray()
                .First();
            var orderBySplit = firstSortOrderItem
                .Split(" ")
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();
            var s = SortOrder.From(orderBySplit[1]);
            return new SplitOrderByPhrase
            {
                PropertyName = orderBySplit[0],
                Direction = s.DeveloperName,
            };
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }
}
