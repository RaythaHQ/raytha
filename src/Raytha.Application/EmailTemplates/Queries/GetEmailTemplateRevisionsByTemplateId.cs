using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.EmailTemplates.Queries;

public class GetEmailTemplateRevisionsByTemplateId
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<EmailTemplateRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<EmailTemplateRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<EmailTemplateRevisionDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.EmailTemplateRevisions.AsQueryable()
                .Include(p => p.EmailTemplate)
                .Include(p => p.CreatorUser)
                .Where(p => p.EmailTemplateId == request.Id.Guid);

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(EmailTemplateRevisionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<EmailTemplateRevisionDto>>(new ListResultDto<EmailTemplateRevisionDto>(items, total));
        }
    }
}
