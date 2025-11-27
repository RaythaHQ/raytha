using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplateById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<WidgetTemplateDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WidgetTemplateDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<WidgetTemplateDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .WidgetTemplates.Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Widget Template", request.Id);

            return new QueryResponseDto<WidgetTemplateDto>(WidgetTemplateDto.GetProjection(entity)!);
        }
    }
}

