using System;

namespace Raytha.Web.Areas.Admin.Views.Dashboard;

public class Dashboard_ViewModel
{
    public decimal CurrentFileStorageSizeInGb { get; set; }
    public int CurrentTotalContentItems { get; set; }
    public int CurrentTotalUsers { get; set; }

    public string FileStorageProvider { get; set; }
    public decimal MaxTotalDiskSpaceInGb { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }

    public decimal FileStoragePercentUsed => Math.Round((CurrentFileStorageSizeInGb / MaxTotalDiskSpaceInGb) * 100, 2);
 
}
