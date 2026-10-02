using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles.Commands;

[WebhookEvent("role.created", DisplayName = "Role created", Group = "Roles")]
public class CreateRole
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public IEnumerable<string> SystemPermissions { get; init; } = null!;
        public Dictionary<string, IEnumerable<string>> ContentTypePermissions { get; init; } =
            null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, ICurrentUser currentUser)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName)
                .Must(StringExtensions.IsValidDeveloperName)
                .WithMessage("Invalid developer name.");
            RuleFor(x => x.DeveloperName)
                .Must(
                    (request, developerName) =>
                    {
                        var entity = db.Roles.FirstOrDefault(p =>
                            p.DeveloperName == request.DeveloperName.ToDeveloperName()
                        );
                        return !(entity != null);
                    }
                )
                .WithMessage("A role with that developer name already exists.");
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                        context.AddDenial(
                            AdminAuthorityGuard.CheckRoleDefinition(
                                db.FindCaller(currentUser),
                                null,
                                PermissionGrant.FromRequest(
                                    request.SystemPermissions,
                                    request.ContentTypePermissions
                                )
                            )
                        )
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
            var grant = PermissionGrant.FromRequest(
                request.SystemPermissions,
                request.ContentTypePermissions
            );

            Role entity = new Role
            {
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                SystemPermissions = grant.System,
                ContentTypeRolePermissions = grant.ToContentTypeRolePermissions().ToList(),
            };

            _db.Roles.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
