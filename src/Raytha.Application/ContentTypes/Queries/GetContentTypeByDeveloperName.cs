using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypeByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<ContentTypeDto>>
    {
        public string DeveloperName { get; init; } = null!;
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ContentTypeDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ContentTypeDto> Handle(Query request)
        {
            var entity = _db.ContentTypes
                .Include(p => p.ContentTypeFields.OrderBy(c => c.FieldOrder))
                .FirstOrDefault(p => p.DeveloperName == request.DeveloperName.ToDeveloperName());

            if (entity == null)
                throw new NotFoundException("Content type", request.DeveloperName.ToDeveloperName());

            return new QueryResponseDto<ContentTypeDto>(ContentTypeDto.GetProjection(entity));
        }
    }
}
