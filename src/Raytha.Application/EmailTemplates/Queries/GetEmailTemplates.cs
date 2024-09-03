using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.EmailTemplates.Queries;

public class GetEmailTemplates
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<EmailTemplateDto>>>
    {
        public override string OrderBy { get; init; } = $"Subject {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<EmailTemplateDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<EmailTemplateDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.EmailTemplates
                .Include(p => p.LastModifierUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query
                    .Where(d =>
                        d.Subject.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.ToLower().Contains(searchQuery) ||
                        d.LastModifierUser.FirstName.ToLower().Contains(searchQuery) ||
                        d.LastModifierUser.LastName.ToLower().Contains(searchQuery));
            }

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(EmailTemplateDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<EmailTemplateDto>>(new ListResultDto<EmailTemplateDto>(items, total));
        }
    }
}
