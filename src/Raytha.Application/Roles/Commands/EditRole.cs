using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles.Commands;

[WebhookEvent("role.updated", DisplayName = "Role updated", Group = "Roles")]
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
        public Validator(IRaythaDbContext db, ICurrentUser currentUser)
        {
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
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var existing = db.FindRoleGrants([request.Id.Guid]).FirstOrDefault();
                        if (existing is null)
                            return;

                        context.AddDenial(
                            AdminAuthorityGuard.CheckRoleDefinition(
                                db.FindCaller(currentUser),
                                existing,
                                PermissionGrant.FromRequest(
                                    request.SystemPermissions,
                                    request.ContentTypePermissions
                                )
                            )
                        );
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
                .Roles.Include(p => p.ContentTypeRolePermissions)
                .FirstOrDefault(p => p.Id == request.Id.Guid);
            if (entity == null)
                throw new NotFoundException("Role", request.Id);

            var grant = PermissionGrant.FromRequest(
                request.SystemPermissions,
                request.ContentTypePermissions
            );

            entity.Label = request.Label;
            entity.SystemPermissions = grant.System;
            entity.ContentTypeRolePermissions.Clear();
            foreach (var permission in grant.ToContentTypeRolePermissions())
            {
                entity.ContentTypeRolePermissions.Add(permission);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
