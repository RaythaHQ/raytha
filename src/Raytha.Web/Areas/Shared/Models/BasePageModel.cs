using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Web.Areas.Shared.Models;

public abstract class BasePageModel : PageModel
{
    public const string ErrorMessageKey = "ErrorMessage";
    public const string SuccessMessageKey = "SuccessMessage";
    public const string WarningMessageKey = "WarningMessage";

    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private IFileStorageProviderSettings _fileStorageSettings;
    private IFileStorageProvider _fileStorageProvider;
    private IEmailer _emailer;
    private IEmailerConfiguration _emailerConfiguration;
    private IRelativeUrlBuilder _relativeUrlBuilder;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization =>
        _currentOrganization ??=
            HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IFileStorageProvider FileStorageProvider =>
        _fileStorageProvider ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProvider>();
    protected IFileStorageProviderSettings FileStorageProviderSettings =>
        _fileStorageSettings ??=
            HttpContext.RequestServices.GetRequiredService<IFileStorageProviderSettings>();
    protected IEmailer Emailer =>
        _emailer ??= HttpContext.RequestServices.GetRequiredService<IEmailer>();
    protected IEmailerConfiguration EmailerConfiguration =>
        _emailerConfiguration ??=
            HttpContext.RequestServices.GetRequiredService<IEmailerConfiguration>();
    protected IRelativeUrlBuilder RelativeUrlBuilder =>
        _relativeUrlBuilder ??=
            HttpContext.RequestServices.GetRequiredService<IRelativeUrlBuilder>();

    public Dictionary<string, string> ValidationFailures { get; set; }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next
    )
    {
        if (CurrentOrganization.RedirectWebsite.IsValidUriFormat())
        {
            context.HttpContext.Response.Redirect(CurrentOrganization.RedirectWebsite);
            return;
        }

        string currentPage = RouteData.Values["page"]?.ToString();
        if (
            !CurrentOrganization.InitialSetupComplete
            && !string.IsNullOrEmpty(currentPage)
            && currentPage != "/Setup/Index"
        )
        {
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
            string search = context.HttpContext.Request.Query["search"];
            int pageNumber = Convert.ToInt32(context.HttpContext.Request.Query["pageNumber"]);
            int pageSize = Convert.ToInt32(context.HttpContext.Request.Query["pageSize"]);
            pageSize = pageSize == 0 ? 50 : pageSize;
            string orderBy = context.HttpContext.Request.Query["orderBy"].ToString().Trim();
            string filter = context.HttpContext.Request.Query["filter"];
            string actionName = context.RouteData.Values["page"].ToString();

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
                    paginationModel.PageName = actionName;

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

    protected void SetErrorMessage(string message)
    {
        ViewData[ErrorMessageKey] = message;
        TempData[ErrorMessageKey] = message;
    }

    protected void SetErrorMessage(IEnumerable<ValidationFailure> errors, int statusCode = 303)
    {
        SetErrorMessage(string.Empty, errors, statusCode);
    }

    protected void SetErrorMessage(
        string message,
        IEnumerable<ValidationFailure> errors,
        int statusCode = 303
    )
    {
        var validationSummary = errors.FirstOrDefault(p =>
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

        ValidationFailures = errors?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage);
        HttpContext.Response.StatusCode = statusCode;
    }

    protected void SetSuccessMessage(string message)
    {
        ViewData[SuccessMessageKey] = message;
        TempData[SuccessMessageKey] = message;
    }

    protected void SetWarningMessage(string message)
    {
        ViewData[WarningMessageKey] = message;
        TempData[WarningMessageKey] = message;
    }

    public string HasError(string propertyName)
    {
        return ValidationFailures != null && ValidationFailures.Any(p => p.Key == propertyName)
            ? "is-invalid"
            : string.Empty;
    }

    public string ErrorMessageFor(string propertyName)
    {
        return ValidationFailures?.FirstOrDefault(p => p.Key == propertyName).Value;
    }
}

public interface IHasListView<T>
{
    public ListViewModel<T> ListView { get; set; }
}

public class SplitOrderByPhrase
{
    public string PropertyName { get; set; }
    public string Direction { get; set; }

    public static SplitOrderByPhrase From(string orderByPhrase)
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
