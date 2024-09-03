using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Raytha.Application.Common.Interfaces;

public interface IRaythaDbContext
{
    public DbSet<User> Users { get; }
    public DbSet<Role> Roles { get; }
    public DbSet<UserGroup> UserGroups { get; }
    public DbSet<VerificationCode> VerificationCodes { get; }
    public DbSet<ContentTypeField> ContentTypeFields { get; }
    public DbSet<ContentType> ContentTypes { get; }
    public DbSet<View> Views { get; }
    public DbSet<Domain.Entities.OrganizationSettings> OrganizationSettings { get; }
    public DbSet<WebTemplate> WebTemplates { get; }
    public DbSet<WebTemplateRevision> WebTemplateRevisions { get; }
    public DbSet<WebTemplateAccessToModelDefinition> WebTemplateAccessToModelDefinitions { get; }
    public DbSet<EmailTemplate> EmailTemplates { get; }
    public DbSet<EmailTemplateRevision> EmailTemplateRevisions { get; }
    public DbSet<ContentItem> ContentItems { get; }
    public DbSet<ContentItemRevision> ContentItemRevisions { get; }
    public DbSet<AuthenticationScheme> AuthenticationSchemes { get; }
    public DbSet<JwtLogin> JwtLogins { get; }
    public DbSet<OneTimePassword> OneTimePasswords { get; }
    public DbSet<AuditLog> AuditLogs { get; }
    public DbSet<DeletedContentItem> DeletedContentItems { get; }
    public DbSet<Route> Routes { get; }
    public DbSet<MediaItem> MediaItems { get; }
    public DbSet<ApiKey> ApiKeys { get; }
    public DbSet<BackgroundTask> BackgroundTasks { get; }
    public DbSet<RaythaFunction> RaythaFunctions { get; }
    public DbSet<RaythaFunctionRevision> RaythaFunctionRevisions { get; }
    public DbSet<NavigationMenu> NavigationMenus { get; }
    public DbSet<NavigationMenuRevision> NavigationMenuRevisions { get; }
    public DbSet<NavigationMenuItem> NavigationMenuItems { get; }
    public DbSet<Theme> Themes { get; }
    public DbSet<ThemeAccessToMediaItem> ThemeAccessToMediaItems { get; }
    public DbSet<WebTemplateViewRelation> WebTemplateViewRelations { get; }
    public DbSet<WebTemplateContentItemRelation> WebTemplateContentItemRelations { get; }
    public DbContext DbContext { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
