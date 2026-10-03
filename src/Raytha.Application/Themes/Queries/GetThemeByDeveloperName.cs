using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.Queries;

public class GetThemeByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<ThemeDto>>
    {
        public required string DeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ThemeDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ThemeDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.Themes.AsNoTracking().FirstOrDefaultAsync(
                t => t.DeveloperName == request.DeveloperName.ToDeveloperName(),
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("Theme", request.DeveloperName);

            return new QueryResponseDto<ThemeDto>(ThemeDto.GetProjection(entity));
        }
    }
}
