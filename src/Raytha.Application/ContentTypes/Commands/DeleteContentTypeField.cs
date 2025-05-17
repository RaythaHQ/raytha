using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.ContentTypes.Commands;

public class DeleteContentTypeField
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db
                            .ContentTypeFields.Include(p => p.ContentType)
                            .ThenInclude(p => p.ContentTypeFields)
                            .Include(p => p.ContentType)
                            .ThenInclude(p => p.Views)
                            .FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Content Type Field", request.Id);

                        if (entity.Id == entity.ContentType.PrimaryFieldId)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete a content type's primary field. Set another field as Primary before deleting this one."
                            );
                            return;
                        }

                        var viewsWithFieldInSort = entity.ContentType.Views.Where(p =>
                            p.Sort.Any(c => c.DeveloperName == entity.DeveloperName)
                        );
                        if (viewsWithFieldInSort.Any())
                        {
                            var viewNames = string.Join(
                                "; ",
                                viewsWithFieldInSort.Select(p => p.DeveloperName)
                            );
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"Cannot delete this field because it is used in the sort order configuration of the following views: {viewNames}"
                            );
                            return;
                        }

                        var viewsWithFieldInFilter = entity.ContentType.Views.Where(p =>
                            p.Filter.Any(c => c.Field == entity.DeveloperName)
                        );
                        if (viewsWithFieldInFilter.Any())
                        {
                            var viewNames = string.Join(
                                "; ",
                                viewsWithFieldInFilter.Select(p => p.DeveloperName)
                            );
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"Cannot delete this field because it is used in the filter configuration of the following views: {viewNames}"
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .ContentTypeFields.Include(p => p.ContentType)
                .ThenInclude(p => p.Views)
                .First(p => p.Id == request.Id.Guid);

            _db.ContentTypeFields.Remove(entity);

            var viewsWithFieldInColumns = entity.ContentType.Views.Where(p =>
                p.Columns.Any(c => c == entity.DeveloperName)
            );
            foreach (var view in viewsWithFieldInColumns)
            {
                var columns = view.Columns.ToList();
                columns.Remove(entity.DeveloperName);
                view.Columns = columns;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
