using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Views.Commands;

public class EditPublicSettings
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool IsPublished { get; init; }
        public string RoutePath { get; init; }
        public ShortGuid TemplateId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.RoutePath).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (request.Id == ShortGuid.Empty)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Id is required.");
                    return;
                }

                if (request.TemplateId == ShortGuid.Empty)
                {
                    context.AddFailure("TemplateId", "Template is required.");
                    return;
                }

                var entity = db.Views
                    .Include(p => p.ContentType)
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("View", request.Id);

                var template = db.WebTemplates
                    .Include(p => p.TemplateAccessToModelDefinitions)
                    .FirstOrDefault(p => p.Id == request.TemplateId.Guid);

                if (template == null)
                    throw new NotFoundException("WebTemplate", request.TemplateId);

                if (!template.TemplateAccessToModelDefinitions.Any(p => p.ContentTypeId == entity.ContentType.Id))
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template does not have access to this model definition.");
                    return;
                }

                var slugifiedPath = request.RoutePath.ToUrlSlug();
                if (string.IsNullOrWhiteSpace(slugifiedPath))
                {
                    context.AddFailure("RoutePath", "Invalid route path. Must be letters, numbers, and dashes");
                    return;
                }
                var routePathExists = db.Routes.FirstOrDefault(p => p.Path.ToLower() == slugifiedPath && p.ViewId != request.Id.Guid);
                if (routePathExists != null)
                {
                    context.AddFailure("RoutePath", $"The route path {request.RoutePath.ToUrlSlug()} already exists.");
                    return;
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
            var entity = _db.Views
                .Include(p => p.Route)
                .First(p => p.Id == request.Id.Guid);

            entity.WebTemplateId = request.TemplateId;
            entity.Route.Path = request.RoutePath.ToUrlSlug();
            entity.IsPublished = request.IsPublished;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}