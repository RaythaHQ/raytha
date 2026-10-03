using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.EmailTemplates.Queries;

public class GetEmailTemplateByName
{
    public record Query : IRequest<IQueryResponseDto<EmailTemplateDto>>
    {
        public string DeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<EmailTemplateDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<EmailTemplateDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(
                p => p.DeveloperName == request.DeveloperName.ToDeveloperName(),
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("EmailTemplate", request.DeveloperName);

            return new QueryResponseDto<EmailTemplateDto>(EmailTemplateDto.GetProjection(entity));
        }
    }
}
