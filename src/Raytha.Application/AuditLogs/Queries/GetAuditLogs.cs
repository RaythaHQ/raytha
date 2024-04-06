using FluentValidation;
using MediatR;
using Raytha.Application.Common.Models;
using Raytha.Domain.ValueObjects;
using Raytha.Application.Common.Interfaces;
using CSharpVitamins;
using Raytha.Application.Common.Utils;
using Microsoft.EntityFrameworkCore;

namespace Raytha.Application.AuditLogs.Queries;

public class GetAuditLogs
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<AuditLogDto>>> 
    { 
        public DateTime? StartDateAsUtc { get; init; }
        public DateTime? EndDateAsUtc { get; init; }
        public string Category { get; init; } = null!;
        public ShortGuid? EntityId { get; init; }
        public string EmailAddress { get; init; } = null!;
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Validator : AbstractValidator<Query> {
        public Validator() {
            RuleFor(x => x.EmailAddress).EmailAddress().When(p => !string.IsNullOrEmpty(p.EmailAddress));
        }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<AuditLogDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        
        public async Task<IQueryResponseDto<ListResultDto<AuditLogDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.AuditLogs
                .AsQueryable();
                
            if (request.StartDateAsUtc.HasValue)
                query = query.Where(p => p.CreationTime >= request.StartDateAsUtc);

            if (request.EndDateAsUtc.HasValue)
            {
                var endOfEndDateAsUtc = request.EndDateAsUtc.Value.AddDays(1).AddMilliseconds(-1);
                query = query.Where(p => p.CreationTime <= endOfEndDateAsUtc);
            }

            if (request.EntityId.HasValue && request.EntityId != Guid.Empty)
                query = query.Where(p => p.EntityId == request.EntityId.Value);

            if (!string.IsNullOrEmpty(request.EmailAddress))
            {
                query = query
                    .Where(d => d.UserEmail.ToLower() == request.EmailAddress.ToLower());         
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                query = query
                    .Where(d => d.Category == request.Category);         
            }

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(AuditLogDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<AuditLogDto>>(new ListResultDto<AuditLogDto>(items, total));
        }
    }
}