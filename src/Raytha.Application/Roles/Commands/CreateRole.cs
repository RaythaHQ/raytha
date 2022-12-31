using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles.Commands;

public class CreateRole
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public IEnumerable<string> SystemPermissions { get; init; } = null!;
        public Dictionary<string, IEnumerable<string>> ContentTypePermissions { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).Must(StringExtensions.IsValidDeveloperName).WithMessage("Invalid developer name.");
            RuleFor(x => x.DeveloperName).Must((request, developerName) =>
            {
                var entity = db.Roles.FirstOrDefault(p => p.DeveloperName == request.DeveloperName.ToDeveloperName());
                return !(entity != null);
            }).WithMessage("A role with that developer name already exists.");
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
            var contentTypeRolePermissions = new List<ContentTypeRolePermission>();

            var builtInSystemPermissions = BuiltInSystemPermission.From(request.SystemPermissions.ToArray());

            foreach (var contentTypePermission in request.ContentTypePermissions)
            {
                var contentTypeRolePermission = new ContentTypeRolePermission
                {
                    ContentTypeId = (ShortGuid)contentTypePermission.Key,
                    ContentTypePermissions = BuiltInContentTypePermission.From(contentTypePermission.Value.ToArray())
                };
                contentTypeRolePermissions.Add(contentTypeRolePermission);
            }
            
            Role entity = new Role
            {
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                SystemPermissions = builtInSystemPermissions,
                ContentTypeRolePermissions = contentTypeRolePermissions
            };

            _db.Roles.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
