﻿using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.UserGroups.Queries;

public class GetUserGroupById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<UserGroupDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<UserGroupDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<UserGroupDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.UserGroups
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("UserGroup", request.Id);

            return new QueryResponseDto<UserGroupDto>(UserGroupDto.GetProjection(entity));
        }
    }
}
