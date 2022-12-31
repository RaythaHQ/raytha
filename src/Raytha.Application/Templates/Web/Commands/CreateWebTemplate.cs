using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Templates.Web.Commands;

public class CreateWebTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string DeveloperName { get; init; } = null!;
        public string Label { get; init; } = null!;
        public string Content { get; init; } = null!;
        public bool IsBaseLayout { get; init; }
        public ShortGuid? ParentTemplateId { get; init; }
        public bool AllowAccessForNewContentTypes { get; init; }
        public IEnumerable<ShortGuid> TemplateAccessToModelDefinitions { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).Must(StringExtensions.IsValidDeveloperName).WithMessage("Invalid developer name.");
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().Must(WebTemplateExtensions.HasRenderBodyTag).When(p => p.IsBaseLayout)
                .WithMessage("Content must have the {% renderbody %} tag if it is a base layout.");
            RuleFor(x => x.DeveloperName).Must((request, developerName) =>
            {
                if (string.IsNullOrEmpty(developerName))
                    return true;

                var anyAlreadyExistWithDeveloperName = db.WebTemplates.Any(p => p.DeveloperName == request.DeveloperName.ToDeveloperName());
                return !(anyAlreadyExistWithDeveloperName);
            }).WithMessage("A template with that developer name already exists.");
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
            var template = new WebTemplate
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                Content = request.Content,
                ParentTemplateId = request.ParentTemplateId.HasValue && request.ParentTemplateId != Guid.Empty ? request.ParentTemplateId : null,
                IsBaseLayout = request.IsBaseLayout,
                IsBuiltInTemplate = false,
                AllowAccessForNewContentTypes = request.AllowAccessForNewContentTypes,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                TemplateAccessToModelDefinitions = request.TemplateAccessToModelDefinitions?.Select(p => new WebTemplateAccessToModelDefinition
                {
                    ContentTypeId = p
                }).ToList()
            };
            _db.WebTemplates.Add(template);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(template.Id);
        }
    }
}