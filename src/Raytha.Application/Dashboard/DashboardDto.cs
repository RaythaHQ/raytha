namespace Raytha.Application.Dashboard;

public record DashboardDto
{
    public long FileStorageSize { get; init; }
    public int TotalContentItems { get; init; }
    public int TotalUsers { get; init; }
}
