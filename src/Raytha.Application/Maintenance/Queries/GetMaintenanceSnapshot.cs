using Mediator;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Queries;

public class GetMaintenanceSnapshot
{
    public record Query : IRequest<IQueryResponseDto<MaintenanceSnapshot>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<MaintenanceSnapshot>>
    {
        private readonly IMaintenanceQueryService _maintenance;

        public Handler(IMaintenanceQueryService maintenance)
        {
            _maintenance = maintenance;
        }

        public async ValueTask<IQueryResponseDto<MaintenanceSnapshot>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var snapshot = await _maintenance.GetSnapshotAsync(cancellationToken);
            return new QueryResponseDto<MaintenanceSnapshot>(snapshot);
        }
    }
}
