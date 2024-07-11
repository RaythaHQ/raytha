using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Reflection;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Raytha.Infrastructure.Persistence;

public class RaythaDbContext : DbContext, IRaythaDbContext, IDataProtectionKeyContext
{
    private readonly IMediator _mediator;
    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;

    public RaythaDbContext(
        DbContextOptions<RaythaDbContext> options)
        : base(options)
    {
    }

    public RaythaDbContext(
        DbContextOptions<RaythaDbContext> options,
        IMediator mediator,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
        : base(options)
    {
        _mediator = mediator;
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<ContentTypeField> ContentTypeFields => Set<ContentTypeField>();
    public DbSet<ContentType> ContentTypes => Set<ContentType>();
    public DbSet<View> Views => Set<View>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<WebTemplate> WebTemplates => Set<WebTemplate>();
    public DbSet<WebTemplateRevision> WebTemplateRevisions => Set<WebTemplateRevision>();
    public DbSet<WebTemplateAccessToModelDefinition> WebTemplateAccessToModelDefinitions => Set<WebTemplateAccessToModelDefinition>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailTemplateRevision> EmailTemplateRevisions => Set<EmailTemplateRevision>();
    public DbSet<AuthenticationScheme> AuthenticationSchemes => Set<AuthenticationScheme>();
    public DbSet<JwtLogin> JwtLogins => Set<JwtLogin>();
    public DbSet<OneTimePassword> OneTimePasswords => Set<OneTimePassword>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentItemRevision> ContentItemRevisions => Set<ContentItemRevision>();
    public DbSet<DeletedContentItem> DeletedContentItems => Set<DeletedContentItem>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<BackgroundTask> BackgroundTasks => Set<BackgroundTask>();
    public DbSet<RaythaFunction> RaythaFunctions => Set<RaythaFunction>();
    public DbSet<RaythaFunctionRevision> RaythaFunctionRevisions => Set<RaythaFunctionRevision>();
    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();
    public DbSet<NavigationMenuRevision> NavigationMenuRevisions => Set<NavigationMenuRevision>();
    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<ThemeAccessToMediaItem> ThemeAccessToMediaItems => Set<ThemeAccessToMediaItem>();
    public DbSet<WebTemplateViewRelation> WebTemplateViewRelations => Set<WebTemplateViewRelation>();
    public DbSet<WebTemplateContentItemRelation> WebTemplateContentItemRelations => Set<WebTemplateContentItemRelation>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public DbContext DbContext => DbContext;


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _mediator.DispatchDomainEventsBeforeSaveChanges(this);
        var numItems = await base.SaveChangesAsync(cancellationToken);
        await _mediator.DispatchDomainEventsAfterSaveChanges(this);

        return numItems;
    }
}
