using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.BackgroundTasks.Queries;

public class GetBackgroundTasks
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<BackgroundTaskDto>>>
    {
        public string? Status { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<BackgroundTaskDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<BackgroundTaskDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.BackgroundTasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusName = request.Status.Trim().ToLowerInvariant();
                var status = BackgroundTaskStatus.SupportedTypes.FirstOrDefault(p =>
                    p.DeveloperName == statusName
                );
                if (status != null)
                {
                    query = query.Where(p => p.Status == status);
                }
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search)
                    || (p.ErrorMessage != null && p.ErrorMessage.ToLower().Contains(search))
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(BackgroundTaskDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<BackgroundTaskDto>>(
                new ListResultDto<BackgroundTaskDto>(items, total)
            );
        }
    }
}
