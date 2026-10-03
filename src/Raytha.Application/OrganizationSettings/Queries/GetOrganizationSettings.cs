using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.OrganizationSettings.Queries;

public class GetOrganizationSettings
{
    public record Query : IRequest<IQueryResponseDto<OrganizationSettingsDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<OrganizationSettingsDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<OrganizationSettingsDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var settings = await _db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(
                cancellationToken
            );

            return new QueryResponseDto<OrganizationSettingsDto>(
                OrganizationSettingsDto.GetProjection(settings)
            );
        }
    }
}
