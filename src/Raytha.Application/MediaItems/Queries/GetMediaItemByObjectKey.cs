using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.MediaItems.Queries;

public class GetMediaItemByObjectKey
{
    public record Query : IRequest<IQueryResponseDto<MediaItemDto>>
    {
        public string ObjectKey { get; init; }
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<MediaItemDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<MediaItemDto> Handle(Query request)
        {
            var entity = _db.MediaItems.FirstOrDefault(p => p.ObjectKey == request.ObjectKey);

            if (entity == null)
                throw new NotFoundException("Media item", request.ObjectKey);

            return new QueryResponseDto<MediaItemDto>(MediaItemDto.GetProjection(entity));
        }
    }
}