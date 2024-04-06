﻿using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.MediaItems.Queries;

public class GetMediaItemById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<MediaItemDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<MediaItemDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        
        public async Task<IQueryResponseDto<MediaItemDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.MediaItems.FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Media item", request.Id);

            return new QueryResponseDto<MediaItemDto>(MediaItemDto.GetProjection(entity));
        }
    }
}