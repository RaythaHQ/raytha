using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Queries;

public class GetThemeById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ThemeDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ThemeDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ThemeDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = await _db.Themes
                .FirstOrDefaultAsync(t => t.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Theme", request.Id);

            return new QueryResponseDto<ThemeDto>(ThemeDto.GetProjection(entity));
        }
    }
}