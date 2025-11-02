using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Dashboard.Queries;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Dashboard;

public class Index : BaseAdminPageModel
{
    private const decimal ONE_GIGABYTE = 1000000000;
    private const decimal ONE_MEGABYTE = 1000000;
    
    public decimal CurrentFileStorageSizeInGb { get; set; }
    public decimal CurrentDbSizeInMb { get; set; }
    public int CurrentTotalContentItems { get; set; }
    public int CurrentTotalUsers { get; set; }
    public decimal MaxTotalDiskSpaceInGb { get; set; }
    public decimal MaxTotalDbSizeInMb { get; set; }
    public string AllowedMimeTypes { get; set; } = string.Empty;
    public bool UseDirectUploadToCloud { get; set; }
    
    public string FileStorageProviderName => FileStorageProvider.GetName();
    public decimal FileStoragePercentUsed =>
        MaxTotalDiskSpaceInGb > 0 
            ? Math.Round(CurrentFileStorageSizeInGb / MaxTotalDiskSpaceInGb * 100, 2) 
            : 0;
    public decimal DbPercentUsed => 
        MaxTotalDbSizeInMb > 0 
            ? Math.Round(CurrentDbSizeInMb / MaxTotalDbSizeInMb * 100, 2) 
            : 0;

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Dashboard",
                RouteName = RouteNames.Dashboard.Index,
                IsActive = true,
                Icon = "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M2 10a8 8 0 018-8v8h8a8 8 0 11-16 0z\"></path><path d=\"M12 2.252A8.014 8.014 0 0117.748 8H12V2.252z\"></path></svg>"
            }
        );

        var response = await Mediator.Send(new GetDashboardMetrics.Query());
        
        CurrentFileStorageSizeInGb = Math.Round(response.Result.FileStorageSize / ONE_GIGABYTE, 2);
        CurrentDbSizeInMb = Math.Round(response.Result.DbSize, 2);
        CurrentTotalContentItems = response.Result.TotalContentItems;
        CurrentTotalUsers = response.Result.TotalUsers;
        AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes;
        UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud;
        MaxTotalDiskSpaceInGb = Math.Round(
            FileStorageProviderSettings.MaxTotalDiskSpace / ONE_GIGABYTE,
            2
        );
        MaxTotalDbSizeInMb = Math.Round(
            FileStorageProviderSettings.MaxTotalDbSize / ONE_MEGABYTE,
            2
        );

        return Page();
    }
}
