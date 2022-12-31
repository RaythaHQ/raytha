using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.OrganizationSettings.Queries;

public class GetOrganizationSettings
{
    public record Query : IRequest<IQueryResponseDto<OrganizationSettingsDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<OrganizationSettingsDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<OrganizationSettingsDto> Handle(Query request)
        {
            var settings = _db.OrganizationSettings.FirstOrDefault();

            return new QueryResponseDto<OrganizationSettingsDto>(OrganizationSettingsDto.GetProjection(settings));
        }
    }
}
