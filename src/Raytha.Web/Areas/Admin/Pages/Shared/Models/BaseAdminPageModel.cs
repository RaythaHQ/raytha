using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Security;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

[Area("Admin")]
[Authorize(Policy = RaythaClaimTypes.IsAdmin)]
public abstract class BaseAdminPageModel : BasePageModel { }
