using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes.Commands;

public class CreateContentType
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string LabelPlural { get; init; } = null!;
        public string LabelSingular { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string DefaultRouteTemplate { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.LabelPlural).NotEmpty();
            RuleFor(x => x.LabelSingular).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x.DefaultRouteTemplate).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (db.ContentTypes.Any(p => p.DeveloperName == request.DeveloperName.ToDeveloperName()))
                {
                    context.AddFailure("DeveloperName", $"A content type with the developer name {request.DeveloperName.ToDeveloperName()} already exists.");
                    return;
                }

                if (request.DefaultRouteTemplate.IsProtectedRoutePath())
                {
                    context.AddFailure("DefaultRouteTemplate", "Default route path cannot begin with a protected path.");
                    return;
                }

                var activeThemeId = db.OrganizationSettings
                    .Select(os => os.ActiveThemeId)
                    .First();

                var areTemplatesAvailableForCurrentContentType = db.WebTemplates
                    .Where(wt => wt.ThemeId == activeThemeId)
                    .Any(wt => wt.AllowAccessForNewContentTypes && !wt.IsBaseLayout);

                if (!areTemplatesAvailableForCurrentContentType)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "There are no default web templates accessible for new content types. Set a template to allow access to new content types.");
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
            var newContentTypeId = Guid.NewGuid();
            var primaryFieldId = Guid.NewGuid();
            var entity = new ContentType
            {
                Id = newContentTypeId,
                LabelPlural = request.LabelPlural,
                LabelSingular = request.LabelSingular,
                Description = request.Description,
                DefaultRouteTemplate = request.DefaultRouteTemplate,
                DeveloperName = request.DeveloperName.ToDeveloperName()
            };
            _db.ContentTypes.Add(entity);

            var titlePageField = new ContentTypeField
            {
                Id = primaryFieldId,
                Label = "Title",
                DeveloperName = "title",
                ContentTypeId = newContentTypeId,
                FieldOrder = 1,
                FieldType = BaseFieldType.SingleLineText
            };
            _db.ContentTypeFields.Add(titlePageField);

            entity.PrimaryFieldId = primaryFieldId;

            var contentPageField = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = "Content",
                DeveloperName = "content",
                ContentTypeId = newContentTypeId,
                FieldOrder = 2,
                FieldType = BaseFieldType.Wysiwyg
            };
            _db.ContentTypeFields.Add(contentPageField);

            var newViewId = Guid.NewGuid();
            var newView = new View
            {
                Id = newViewId,
                Label = $"All {entity.LabelPlural.ToLower()}",
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                ContentTypeId = newContentTypeId,
                Route = new Route
                {
                    ViewId = newViewId,
                    Path = $"{request.DeveloperName.ToDeveloperName()}"
                },
                Columns = new[] { BuiltInContentTypeField.PrimaryField.DeveloperName, BuiltInContentTypeField.CreationTime.DeveloperName, BuiltInContentTypeField.Template.DeveloperName },
                IsPublished = true
            };

            _db.Views.Add(newView);

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var defaultWebTemplates = await _db.WebTemplates
                .Where(wt => wt.ThemeId == activeThemeId && !wt.IsBaseLayout && wt.AllowAccessForNewContentTypes)
                .ToArrayAsync(cancellationToken);

            foreach (var webTemplate in defaultWebTemplates)
            {
                var templateAccessModel = new WebTemplateAccessToModelDefinition
                {
                    ContentTypeId = newContentTypeId,
                    WebTemplateId = webTemplate.Id
                };

                await _db.WebTemplateAccessToModelDefinitions.AddAsync(templateAccessModel, cancellationToken);
            }

            var defaultContentListView = defaultWebTemplates.FirstOrDefault(p => p.DeveloperName == BuiltInWebTemplate.ContentItemListViewPage.DeveloperName) ?? defaultWebTemplates.First();

            var webTemplateViewRelation = new WebTemplateViewRelation
            {
                Id = Guid.NewGuid(),
                ViewId = newViewId,
                WebTemplateId = defaultContentListView.Id,
            };

            await _db.WebTemplateViewRelations.AddAsync(webTemplateViewRelation, cancellationToken);

            var roles = _db.Roles
                .Include(p => p.ContentTypeRolePermissions)
                .Where(p => p.SystemPermissions.HasFlag(SystemPermissions.ManageContentTypes));
            foreach (var role in roles)
            {
                role.ContentTypeRolePermissions.Add(new ContentTypeRolePermission
                {
                    ContentTypeId = newContentTypeId,
                    ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
