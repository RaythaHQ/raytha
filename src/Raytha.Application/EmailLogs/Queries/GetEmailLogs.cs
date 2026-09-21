using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.EmailLogs.Queries;

public class GetEmailLogs
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<EmailLogListItemDto>>>
    {
        public DateTime? StartDateAsUtc { get; init; }
        public DateTime? EndDateAsUtc { get; init; }
        public string? ToAddress { get; init; }
        public bool? IsSuccess { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ToAddress)
                .EmailAddress()
                .When(p => !string.IsNullOrEmpty(p.ToAddress));
        }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<EmailLogListItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<EmailLogListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.EmailLogs.AsQueryable();

            if (request.StartDateAsUtc.HasValue)
                query = query.Where(p => p.CreationTime >= request.StartDateAsUtc);

            if (request.EndDateAsUtc.HasValue)
            {
                var endOfEndDateAsUtc = request.EndDateAsUtc.Value.AddDays(1).AddMilliseconds(-1);
                query = query.Where(p => p.CreationTime <= endOfEndDateAsUtc);
            }

            if (!string.IsNullOrEmpty(request.ToAddress))
            {
                var to = request.ToAddress.ToLower();
                query = query.Where(p => p.ToAddress.ToLower() == to);
            }

            if (request.IsSuccess.HasValue)
                query = query.Where(p => p.IsSuccess == request.IsSuccess.Value);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(p =>
                    p.Subject.ToLower().Contains(search) || p.ToAddress.ToLower().Contains(search)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(EmailLogListItemDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<EmailLogListItemDto>>(
                new ListResultDto<EmailLogListItemDto>(items, total)
            );
        }
    }
}
