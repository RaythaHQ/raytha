using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Area("Api")]
public class BaseController : Controller
{
    protected const string RAYTHA_API_VERSION = "1.0";
    protected const string RAYTHA_ROUTE_PREFIX = "raytha/api/" + RAYTHA_API_VERSION;

    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private IFileStorageProviderSettings _fileStorageSettings;
    private IFileStorageProvider _fileStorageProvider;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization => _currentOrganization ??= HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IFileStorageProvider FileStorageProvider => _fileStorageProvider ??= HttpContext.RequestServices.GetRequiredService<IFileStorageProvider>();
    protected IFileStorageProviderSettings FileStorageProviderSettings => _fileStorageSettings ??= HttpContext.RequestServices.GetRequiredService<IFileStorageProviderSettings>();


}