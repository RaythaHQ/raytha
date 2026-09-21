using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Application.Dashboard.Queries;

namespace Raytha.Web.Areas.Admin.Api;

public static class DashboardEndpoints
{
    private const decimal OneGigabyte = 1_000_000_000m;
    private const decimal OneMegabyte = 1_000_000m;

    public static RouteGroupBuilder MapDashboard(this RouteGroupBuilder admin)
    {
        admin.MapGet("/dashboard", Metrics).RequireAuthorization(RaythaClaimTypes.IsAdmin);
        return admin;
    }

    private static async Task<IResult> Metrics(
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings settings
    )
    {
        var response = await mediator.Send(new GetDashboardMetrics.Query());
        if (!response.Success)
        {
            return AdminResults.Problem(response.GetErrors());
        }

        var metrics = response.Result;
        var fileStorageGb = Math.Round(metrics.FileStorageSize / OneGigabyte, 2);
        var dbMb = Math.Round(metrics.DbSize, 2);
        var maxDiskGb = Math.Round(settings.MaxTotalDiskSpace / OneGigabyte, 2);
        var maxDbMb = Math.Round(settings.MaxTotalDbSize / OneMegabyte, 2);

        return Results.Ok(
            new
            {
                totalContentItems = metrics.TotalContentItems,
                totalUsers = metrics.TotalUsers,
                fileStorage = new
                {
                    providerName = fileStorageProvider.GetName(),
                    usedBytes = metrics.FileStorageSize,
                    usedGb = fileStorageGb,
                    maxGb = maxDiskGb,
                    percentUsed = maxDiskGb > 0 ? Math.Round(fileStorageGb / maxDiskGb * 100, 2) : 0,
                    allowedMimeTypes = settings.AllowedMimeTypes,
                    useDirectUploadToCloud = settings.UseDirectUploadToCloud,
                    maxFileSizeBytes = settings.MaxFileSize,
                },
                database = new
                {
                    usedMb = dbMb,
                    maxMb = maxDbMb,
                    percentUsed = maxDbMb > 0 ? Math.Round(dbMb / maxDbMb * 100, 2) : 0,
                },
            }
        );
    }
}
