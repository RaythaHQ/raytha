using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Dashboard.Queries;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

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
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }
    public string FileStorageProviderName => FileStorageProvider.GetName();
    public decimal FileStoragePercentUsed =>
        Math.Round(CurrentFileStorageSizeInGb / MaxTotalDiskSpaceInGb * 100, 2);
    public decimal DbPercentUsed => Math.Round(CurrentDbSizeInMb / MaxTotalDbSizeInMb * 100, 2);

    public async Task<IActionResult> OnGet()
    {
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
