using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Views.Commands;

public class EditSort
{
    public record Command : GetEntityByIdInputDto, IRequest<CommandResponseDto<ShortGuid>>
    {
        public string DeveloperName { get; init; }
        public bool ShowColumn { get; init; }
        public string OrderByDirection { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db
                            .Views.Include(p => p.ContentType)
                            .ThenInclude(c => c.ContentTypeFields)
                            .FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("View", request.Id);

                        try
                        {
                            BuiltInContentTypeField.From(request.DeveloperName);
                        }
                        catch (ReservedContentTypeFieldNotFoundException)
                        {
                            var exists = entity.ContentType.ContentTypeFields.Any(p =>
                                p.DeveloperName == request.DeveloperName
                            );
                            if (!exists)
                            {
                                context.AddFailure(
                                    Constants.VALIDATION_SUMMARY,
                                    $"Developer name not recognized: {request.DeveloperName}"
                                );
                            }
                        }

                        if (request.ShowColumn && string.IsNullOrEmpty(request.OrderByDirection))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"Sort order required."
                            );
                            return;
                        }

                        try
                        {
                            if (request.ShowColumn)
                                SortOrder.From(request.OrderByDirection);
                        }
                        catch (SortOrderNotFoundException)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"Invalid sort order: {request.OrderByDirection}"
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
                .Views.Include(p => p.ContentType)
                .ThenInclude(c => c.ContentTypeFields)
                .First(p => p.Id == request.Id.Guid);

            var developerName = request.DeveloperName;

            var columns = entity.Sort.ToList();
            var columnExists = columns.FirstOrDefault(p => p.DeveloperName == developerName);

            if (request.ShowColumn && columnExists == null)
            {
                columns.Add(
                    new ColumnSortOrder
                    {
                        SortOrder = SortOrder.From(request.OrderByDirection),
                        DeveloperName = developerName,
                    }
                );
            }
            else if (!request.ShowColumn && columnExists != null)
            {
                columns.Remove(
                    new ColumnSortOrder
                    {
                        SortOrder = columnExists.SortOrder,
                        DeveloperName = developerName,
                    }
                );
            }

            entity.Sort = columns;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
