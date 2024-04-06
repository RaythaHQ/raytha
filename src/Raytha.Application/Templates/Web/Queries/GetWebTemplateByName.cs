using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Templates.Email;

namespace Raytha.Application.Templates.Web.Queries;

public class GetWebTemplateByName
{
    public record Query : IRequest<IQueryResponseDto<WebTemplateDto>>
    {
        public string DeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebTemplateDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<WebTemplateDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.WebTemplates
                .Include(p => p.TemplateAccessToModelDefinitions)
                .ThenInclude(p => p.ContentType)
                .Include(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .ThenInclude(p => p.ParentTemplate)
                .FirstOrDefault(p => p.DeveloperName == request.DeveloperName.ToDeveloperName());

            if (entity == null)
                throw new NotFoundException("WebTemplate", request.DeveloperName);

            return new QueryResponseDto<WebTemplateDto>(WebTemplateDto.GetProjection(entity));
        }
    }
}
