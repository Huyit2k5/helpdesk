using Helpdesk.CannedResponses;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Priorities;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace Helpdesk.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class HelpdeskDbContext :
    AbpDbContext<HelpdeskDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    // Master Data
    public DbSet<Category> Categories { get; set; }
    public DbSet<Priority> Priorities { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<TicketStatus> TicketStatuses { get; set; }
    public DbSet<TicketSource> TicketSources { get; set; }
    public DbSet<CannedResponse> CannedResponses { get; set; }

    // Tickets
    public DbSet<Helpdesk.Tickets.Ticket> Tickets { get; set; }
    public DbSet<Helpdesk.Tickets.TicketComment> TicketComments { get; set; }
    public DbSet<Helpdesk.Tickets.TicketAttachment> TicketAttachments { get; set; }
    public DbSet<Helpdesk.Tickets.TicketActivity> TicketActivities { get; set; }

    // SLA
    public DbSet<Helpdesk.Sla.SlaPolicy> SlaPolicies { get; set; }
    public DbSet<Helpdesk.Sla.SlaPolicyRule> SlaPolicyRules { get; set; }
    public DbSet<Helpdesk.Sla.BusinessHour> BusinessHours { get; set; }
    public DbSet<Helpdesk.Sla.Holiday> Holidays { get; set; }
    public DbSet<Helpdesk.Sla.SlaBreachLog> SlaBreachLogs { get; set; }

    // Knowledge Base
    public DbSet<Helpdesk.KnowledgeBase.KnowledgeArticle> KnowledgeArticles { get; set; }

    // Assignment Rules
    public DbSet<Helpdesk.AssignmentRules.AssignmentRule> AssignmentRules { get; set; }
    public DbSet<Helpdesk.AssignmentRules.AssignmentRuleAgent> AssignmentRuleAgents { get; set; }

    // Notifications
    public DbSet<Helpdesk.Notifications.Notification> Notifications { get; set; }

    // Automations & Macros
    public DbSet<Helpdesk.Automations.AutomationRule> AutomationRules { get; set; }
    public DbSet<Helpdesk.Automations.Macro> Macros { get; set; }

    // IT Asset Management & CMDB
    public DbSet<Helpdesk.Assets.Asset> Assets { get; set; }
    public DbSet<Helpdesk.Assets.AssetActivity> AssetActivities { get; set; }
    public DbSet<Helpdesk.Assets.AssetMaintenance> AssetMaintenances { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        builder.ConfigureHelpdesk();
    }
}
