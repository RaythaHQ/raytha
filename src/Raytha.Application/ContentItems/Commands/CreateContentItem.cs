using FluentValidation;
using MediatR;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using CSharpVitamins;
using Raytha.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Events;
using Raytha.Application.Common.Attributes;

namespace Raytha.Application.ContentItems.Commands;

public class CreateContentItem
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public bool SaveAsDraft { get; init; }
        public ShortGuid TemplateId { get; init; }

        [ExcludePropertyFromOpenApiDocs]
        public string ContentTypeDeveloperName { get; init; } = string.Empty;
        public IDictionary<string, dynamic> Content { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                if (string.IsNullOrEmpty(request.ContentTypeDeveloperName))
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "ContentTypeDeveloperName is required.");
                    return;
                }

                if (request.TemplateId == ShortGuid.Empty)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "TemplateId is required.");
                    return;
                }

                var contentTypeDefinition = db.ContentTypes
                    .Include(p => p.ContentTypeFields)
                    .FirstOrDefault(p => p.DeveloperName == request.ContentTypeDeveloperName.ToDeveloperName());

                if (contentTypeDefinition == null)
                    throw new NotFoundException("Content Type", request.ContentTypeDeveloperName.ToDeveloperName());

                var template = db.WebTemplates
                    .Where(wt => wt.Id == request.TemplateId.Guid)
                    .Select(wt => new { wt.ThemeId, wt.TemplateAccessToModelDefinitions })
                    .FirstOrDefault();

                if (template == null)
                    throw new NotFoundException("WebTemplate", request.TemplateId);

                var activeThemeId = db.OrganizationSettings
                    .Select(os => os.ActiveThemeId)
                    .First();

                if (template.ThemeId != activeThemeId)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template does not apply to the current active theme.");
                    return;
                }

                if (!template.TemplateAccessToModelDefinitions.Any(p => p.ContentTypeId == contentTypeDefinition.Id))
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template does not have access to this model definition.");
                    return;
                }

                foreach (var field in request.Content)
                {
                    var fieldDefinition = contentTypeDefinition.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == field.Key);
                    if (fieldDefinition == null)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"{field.Key} is not a recognized field for content type: {contentTypeDefinition.LabelSingular}");
                    }
                    else
                    {
                        try
                        {
                            var fieldValue = fieldDefinition.FieldType.FieldValueFrom(field.Value);
                            if (!request.SaveAsDraft && fieldDefinition.IsRequired && !fieldValue.HasValue)
                            {
                                context.AddFailure(fieldDefinition.DeveloperName, $"'{fieldDefinition.Label}' field is required.");
                            }
                        }
                        catch (Exception ex)
                        {
                            context.AddFailure(fieldDefinition.DeveloperName, $"'{fieldDefinition.Label}' is an invalid format.");
                        }
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
            var contentTypeDefinition = _db.ContentTypes
                .Include(p => p.ContentTypeFields)
                .First(p => p.DeveloperName == request.ContentTypeDeveloperName.ToDeveloperName());

            var newEntityId = Guid.NewGuid();
            var path = GetRoutePath(request.Content, newEntityId, contentTypeDefinition.Id);
            var entity = new ContentItem
            {
                Id = newEntityId,
                IsDraft = request.SaveAsDraft,
                IsPublished = request.SaveAsDraft == false,
                DraftContent = request.Content,
                PublishedContent = request.Content,
                ContentTypeId = contentTypeDefinition.Id,
                Route = new Route
                {
                    Path = path,
                    ContentItemId = newEntityId
                }
            };

            _db.ContentItems.Add(entity);

            var webTemplateContentItemRelation = new WebTemplateContentItemRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = request.TemplateId.Guid,
                ContentItemId = entity.Id,
            };

            await _db.WebTemplateContentItemRelations.AddAsync(webTemplateContentItemRelation, cancellationToken);

            entity.AddDomainEvent(new ContentItemCreatedEvent(entity));

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }

        public string GetRoutePath(dynamic content, Guid entityId, Guid contentTypeId)
        {
            var contentType = _db.ContentTypes
                .Include(p => p.ContentTypeFields)
                .First(p => p.Id == contentTypeId);

            var routePathTemplate = contentType.DefaultRouteTemplate;

            string primaryFieldDeveloperName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;
            var primaryField = ((IDictionary<string, dynamic>)content)[primaryFieldDeveloperName] as string;

            string path = routePathTemplate.IfNullOrEmpty($"{BuiltInContentTypeField.PrimaryField.DeveloperName}")
                                           .Replace($"{{{BuiltInContentTypeField.PrimaryField.DeveloperName}}}", primaryField.IfNullOrEmpty((ShortGuid)entityId))
                                           .Replace($"{{{BuiltInContentTypeField.Id.DeveloperName}}}", (ShortGuid)entityId)
                                           .Replace("{ContentTypeDeveloperName}", contentType.DeveloperName)
                                           .Replace("{CurrentYear}", DateTime.UtcNow.Year.ToString())
                                           .Replace("{CurrentMonth}", DateTime.UtcNow.Month.ToString());

            path = path.ToUrlSlug().Truncate(200, string.Empty);

            if (_db.Routes.Any(p => p.Path == path))
            {
                path = $"{(ShortGuid)entityId}-{path}".Truncate(200, string.Empty);
            }

            return path;
        }
    }
}
