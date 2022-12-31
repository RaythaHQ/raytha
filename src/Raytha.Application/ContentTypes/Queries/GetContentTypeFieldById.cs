using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypeFieldById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ContentTypeFieldDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ContentTypeFieldDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        protected override IQueryResponseDto<ContentTypeFieldDto> Handle(Query request)
        {
            var entity = _db.ContentTypeFields
                .Include(p => p.ContentType)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Type Field", request.Id);

            return new QueryResponseDto<ContentTypeFieldDto>(ContentTypeFieldDto.GetProjection(entity));
        }
    }
}
