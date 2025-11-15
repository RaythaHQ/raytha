using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypeById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ContentTypeDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ContentTypeDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ContentTypeDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .ContentTypes.Include(p => p.ContentTypeFields.OrderBy(c => c.FieldOrder))
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content type", request.Id);

            return new QueryResponseDto<ContentTypeDto>(ContentTypeDto.GetProjection(entity));
        }
    }
}
