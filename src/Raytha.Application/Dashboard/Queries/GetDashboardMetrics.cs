using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Dashboard.Queries;

public class GetDashboardMetrics
{
    public record Query : IRequest<IQueryResponseDto<DashboardDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<DashboardDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<DashboardDto> Handle(Query request)
        {
            int totalContentItems = _db.ContentItems.Count();
            int totalUsers = _db.Users.Count();
            long totalFileStorageSize = _db.MediaItems.Sum(p => p.Length);
            return new QueryResponseDto<DashboardDto>(
                new DashboardDto
                {
                    TotalContentItems = totalContentItems,
                    TotalUsers = totalUsers,
                    FileStorageSize = totalFileStorageSize,
                });
        }
    }

    public class CSpaceUsed
    {
        string database_size { get; set; }
    }
}
