using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Queries;

public class GetSitePageById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<SitePageDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SitePageDto>>
    {
        public const string DefaultSectionName = "main";

        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SitePageDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .SitePages.AsNoTracking()
                .Include(p => p.Route)
                .IncludeParentTemplates(p => p.WebTemplate)
                .Include(p => p.CreatorUser)
                .Include(p => p.LastModifierUser)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Site Page", request.Id);

            return new QueryResponseDto<SitePageDto>(
                SitePageDto.GetProjection(entity) with
                {
                    TemplateSections = TemplateSections(entity.WebTemplate),
                }
            );
        }

        public static IReadOnlyList<string> TemplateSections(WebTemplate? template)
        {
            var sections = new List<string>();
            var visited = new HashSet<Guid>();
            for (
                var current = template;
                current != null && visited.Add(current.Id);
                current = current.ParentTemplate
            )
            {
                foreach (var name in TemplateSectionParser.ExtractSectionNames(current.Content))
                {
                    if (!sections.Contains(name))
                    {
                        sections.Add(name);
                    }
                }
            }
            return sections.Count > 0 ? sections : new List<string> { DefaultSectionName };
        }
    }
}
