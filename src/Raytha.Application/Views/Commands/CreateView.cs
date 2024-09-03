using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Views.Commands;

public class CreateView
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid ContentTypeId { get; init; }
        public string Label { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public string Description { get; init; } = null!;
        public ShortGuid? DuplicateFromId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty().Must(StringExtensions.IsValidDeveloperName).WithMessage("Invalid developer name.");
            RuleFor(x => x).Custom((request, context) =>
            {
                var contentType = db.ContentTypes.FirstOrDefault(p => p.Id == request.ContentTypeId.Guid);
                if (contentType == null)
                    throw new NotFoundException("Content Type", request.ContentTypeId);

                if (request.DuplicateFromId.HasValue && request.DuplicateFromId.Value != ShortGuid.Empty)
                {
                    var duplicateExists = db.Views.Any(p => p.Id == request.DuplicateFromId.Value.Guid);
                    if (!duplicateExists)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"View {request.DuplicateFromId} does not exist. Cannot perform duplication.");
                        return;
                    }
                }

                if (db.Views.Any(p => p.ContentTypeId == request.ContentTypeId.Guid && p.DeveloperName == request.DeveloperName.ToDeveloperName()))
                {
                    context.AddFailure("DeveloperName", $"A view with the developer name of {request.DeveloperName.ToDeveloperName()} already exists.");
                    return;
                }

                if (request.DeveloperName.Length > 64)
                {
                    context.AddFailure("DeveloperName", $"Developer name cannot be longer than 64 characters.");
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
            var newEntityId = Guid.NewGuid();

            var entity = new View
            {
                Id = newEntityId,
                ContentTypeId = request.ContentTypeId.Guid,
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                Description = request.Description
            };

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            if (request.DuplicateFromId != ShortGuid.Empty && request.DuplicateFromId.HasValue)
            {
                var originalView = _db.Views.First(p => p.Id == request.DuplicateFromId.Value.Guid);
                entity.Columns = originalView.Columns;
                entity.Filter = originalView.Filter;
                entity.Sort = originalView.Sort;
                entity.DefaultNumberOfItemsPerPage = originalView.DefaultNumberOfItemsPerPage;
                entity.MaxNumberOfItemsPerPage = originalView.MaxNumberOfItemsPerPage;
                entity.IgnoreClientFilterAndSortQueryParams = originalView.IgnoreClientFilterAndSortQueryParams;

                var originalViewWebTemplateId = await _db.WebTemplateViewRelations
                    .Where(wtr => wtr.ViewId == originalView.Id && wtr.WebTemplate!.ThemeId == activeThemeId)
                    .Select(wtv => wtv.WebTemplateId)
                    .FirstAsync(cancellationToken);

                var webTemplateViewRelation = new WebTemplateViewRelation
                {
                    Id = Guid.NewGuid(),
                    WebTemplateId = originalViewWebTemplateId,
                    ViewId = entity.Id,
                };

                await _db.WebTemplateViewRelations.AddAsync(webTemplateViewRelation, cancellationToken);
            }
            else
            {
                entity.DefaultNumberOfItemsPerPage = 25;
                entity.MaxNumberOfItemsPerPage = 1000;
                entity.IgnoreClientFilterAndSortQueryParams = false;

                var allFieldsForContentType = _db.ContentTypeFields
                    .Where(p => p.ContentTypeId == request.ContentTypeId.Guid &&
                            !p.IsDeleted).OrderBy(p => p.FieldOrder);

                var chosenColumns = new List<ColumnSortOrder>();
                foreach (var field in allFieldsForContentType)
                {
                    chosenColumns.Add(new ColumnSortOrder
                    {
                        DeveloperName = field.DeveloperName,
                        SortOrder = SortOrder.Ascending
                    });
                }

                entity.Columns = chosenColumns.Select(p => p.DeveloperName);

                var defaultTemplateId = await _db.WebTemplates
                    .Where(wt => wt.ThemeId == activeThemeId && wt.DeveloperName == BuiltInWebTemplate.ContentItemListViewPage.DeveloperName)
                    .Select(wt => wt.Id)
                    .FirstAsync(cancellationToken);

                var webTemplateViewRelation = new WebTemplateViewRelation
                {
                    Id = Guid.NewGuid(),
                    ViewId = entity.Id,
                    WebTemplateId = defaultTemplateId,
                };

                await _db.WebTemplateViewRelations.AddAsync(webTemplateViewRelation, cancellationToken);
            }

            var path = GetRoutePath(request.DeveloperName, newEntityId, request.ContentTypeId.Guid);
            entity.Route = new Route
            {
                Path = path,
                ViewId = newEntityId
            };

            _db.Views.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }

        public string GetRoutePath(string developerName, Guid entityId, Guid contentTypeId)
        {
            var contentType = _db.ContentTypes
                .Include(p => p.ContentTypeFields)
                .First(p => p.Id == contentTypeId);

            string path = string.Empty;

            path = $"{contentType.DeveloperName}/{developerName}".Truncate(200, string.Empty);

            path = path.ToUrlSlug();
            if (_db.Routes.Any(p => p.Path == path))
            {
                path = $"{contentType.DeveloperName}/{(ShortGuid)entityId}-{developerName}".Truncate(200, string.Empty);
                path = path.ToUrlSlug();
            }

            return path;
        }
    }
}
