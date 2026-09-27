using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Queries;

public class GetLogRetention
{
    public record Query : IRequest<IQueryResponseDto<LogRetentionDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<LogRetentionDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<LogRetentionDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var settings = await _db.OrganizationSettings.AsNoTracking().FirstAsync(cancellationToken);
            return new QueryResponseDto<LogRetentionDto>(LogRetentionDto.GetProjection(settings));
        }
    }
}
