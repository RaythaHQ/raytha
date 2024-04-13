using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.BackgroundTasks.Queries;

public class GetBackgroundTaskById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<BackgroundTaskDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<BackgroundTaskDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        
        public async Task<IQueryResponseDto<BackgroundTaskDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.BackgroundTasks.FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Background Task", request.Id);

            return new QueryResponseDto<BackgroundTaskDto>(BackgroundTaskDto.GetProjection(entity));
        }
    }
}
