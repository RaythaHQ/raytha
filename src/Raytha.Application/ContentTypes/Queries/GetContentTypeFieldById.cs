using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypeFieldById
{
    public record Query
        : GetEntityByIdInputDto,
            IRequest<IQueryResponseDto<ContentTypeFieldDto>>
    { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ContentTypeFieldDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ContentTypeFieldDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .ContentTypeFields.AsNoTracking()
                .Include(p => p.ContentType)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Content Type Field", request.Id);

            return new QueryResponseDto<ContentTypeFieldDto>(
                ContentTypeFieldDto.GetProjection(entity)
            );
        }
    }
}
