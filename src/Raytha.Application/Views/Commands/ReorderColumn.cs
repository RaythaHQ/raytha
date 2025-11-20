using System.Collections.ObjectModel;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Application.Views.Commands;

public class ReorderColumn
{
    public record Command : GetEntityByIdInputDto, IRequest<CommandResponseDto<ShortGuid>>
    {
        public string DeveloperName { get; init; }
        public int NewFieldOrder { get; init; }
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
                                return;
                            }
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

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .Views.Include(p => p.ContentType)
                .ThenInclude(c => c.ContentTypeFields)
                .First(p => p.Id == request.Id.Guid);

            var developerName = request.DeveloperName;

            var columns = entity.Columns.ToList();
            int originalColumnOrder = columns.IndexOf(developerName);
            var newColumnOrder = request.NewFieldOrder - 1; //convert to 0-index

            if (originalColumnOrder == -1 || originalColumnOrder == newColumnOrder)
                return new CommandResponseDto<ShortGuid>(entity.Id);

            ObservableCollection<string> columnsAsMovable = new ObservableCollection<string>(
                columns
            );
            columnsAsMovable.Move(originalColumnOrder, newColumnOrder);
            entity.Columns = columnsAsMovable;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
