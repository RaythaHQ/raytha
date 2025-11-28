using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles.Commands;

public class EditRole
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public IEnumerable<string> SystemPermissions { get; init; } = null!;
        public Dictionary<string, IEnumerable<string>> ContentTypePermissions { get; init; } =
            null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Id)
                .Must(id =>
                {
                    var entity = db.Roles.FirstOrDefault(p => p.Id == id.Guid);
                    return entity == null || entity.DeveloperName != BuiltInRole.SuperAdmin;
                })
                .WithMessage("The Super Admin role cannot be edited.");
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.SystemPermissions)
                .Must(permissions =>
                {
                    var permissionsList = permissions?.ToList() ?? new List<string>();
                    var hasSystemSettings = permissionsList.Contains(
                        BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
                    );
                    var hasAdministrators = permissionsList.Contains(
                        BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION
                    );
                    // Both must be selected together or neither
                    return hasSystemSettings == hasAdministrators;
                })
                .WithMessage(
                    "Manage System Settings and Manage Administrators permissions must be selected together."
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
                .Roles.Include(p => p.ContentTypeRolePermissions)
                .FirstOrDefault(p => p.Id == request.Id.Guid);
            if (entity == null)
                throw new NotFoundException("Role", request.Id);

            // Prevent any edits to the Super Admin role
            if (entity.DeveloperName == BuiltInRole.SuperAdmin)
                throw new InvalidOperationException("The Super Admin role cannot be edited.");

            entity.Label = request.Label;
            entity.SystemPermissions = BuiltInSystemPermission.From(
                request.SystemPermissions.ToArray()
            );
            entity.ContentTypeRolePermissions.Clear();

            foreach (var permission in request.ContentTypePermissions)
            {
                var permissionAsEnum = BuiltInContentTypePermission.From(
                    permission.Value.ToArray()
                );
                var newContentTypePermission = new ContentTypeRolePermission
                {
                    ContentTypeId = (ShortGuid)permission.Key,
                    ContentTypePermissions = permissionAsEnum,
                };
                entity.ContentTypeRolePermissions.Add(newContentTypePermission);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
