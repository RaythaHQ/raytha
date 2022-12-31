using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Templates.Email;

namespace Raytha.Application.Templates.Web.Queries;

public class GetWebTemplateById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<WebTemplateDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<WebTemplateDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<WebTemplateDto> Handle(Query request)
        {
            var entity = _db.WebTemplates
                .Include(p => p.TemplateAccessToModelDefinitions)
                .ThenInclude(p => p.ContentType)
                .Include(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("WebTemplate", request.Id);

            return new QueryResponseDto<WebTemplateDto>(WebTemplateDto.GetProjection(entity));
        }
    }
}
