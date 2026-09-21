using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.EmailLogs.Queries;

public class GetEmailLogById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<EmailLogDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<EmailLogDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<EmailLogDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.EmailLogs.FirstOrDefaultAsync(
                p => p.Id == request.Id.Guid,
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("Email log", request.Id);

            return new QueryResponseDto<EmailLogDto>(EmailLogDto.GetProjection(entity));
        }
    }
}
