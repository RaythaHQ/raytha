using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Templates.Web.Commands;

public class DeleteWebTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.WebTemplates.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("Template", request.Id);

                var anyContentItemsUsingTemplate = db.ContentItems.Any(p => p.WebTemplateId == entity.Id);
                if (anyContentItemsUsingTemplate)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template is currently being used by content items. You must change the template those content items are using before deleting this one.");
                    return;
                }

                var anyViewsUsingTemplate = db.Views.Any(p => p.WebTemplateId == entity.Id);
                if (anyViewsUsingTemplate)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template is currently being used by list views. You must change the template those list views are using before deleting this one.");
                    return;
                }

                if (entity.IsBuiltInTemplate)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot remove built-in templates.");
                    return;
                }

                var hasChildTemplates = db.WebTemplates.Any(p => p.ParentTemplateId == entity.Id);
                if (hasChildTemplates)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You must first remove or re-assign child templates.");
                    return;
                }

                var nonBaseLayoutsAllowedForNewTypes = db.WebTemplates.Where(p => !p.IsBaseLayout && p.AllowAccessForNewContentTypes);
                if (nonBaseLayoutsAllowedForNewTypes.Count() == 1)
                {
                    var lastTemplateThatAllowsNewContentTypes = nonBaseLayoutsAllowedForNewTypes.First();
                    if (entity.Id == request.Id.Guid)
                    {
                        context.AddFailure("AllowAccessForNewContentTypes", "This is currently the only template that new content types can access. You must have at least 1 non base layout template new content types can default to.");
                        return;                   
                    }
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.WebTemplates.First(p => p.Id == request.Id.Guid);

            _db.WebTemplates.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
