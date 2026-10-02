using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.EmailTemplates.Queries;

public class GetEmailTemplateById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<EmailTemplateDto>> { }

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
            var entity = _db.EmailTemplates.FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("EmailTemplate", request.Id);

            var dto = EmailTemplateDto.GetProjection(entity);
            return new QueryResponseDto<EmailTemplateDto>(
                dto with
                {
                    AvailableVariables = TemplateInsertVariables.ForEmail(entity.DeveloperName),
                }
            );
        }
    }
}
