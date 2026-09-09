using Helpdesk.CannedResponses;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Priorities;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Helpdesk.EntityFrameworkCore;

public static class HelpdeskDbContextModelCreatingExtensions
{
    public static void ConfigureHelpdesk(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<Category>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Categories", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(CategoryConsts.MaxNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(CategoryConsts.MaxCodeLength);
            b.Property(x => x.Description).HasMaxLength(CategoryConsts.MaxDescriptionLength);

            b.HasIndex(x => x.Code);
            b.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Priority>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Priorities", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(PriorityConsts.MaxNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(PriorityConsts.MaxCodeLength);
            b.Property(x => x.Color).HasMaxLength(PriorityConsts.MaxColorLength);

            b.HasIndex(x => x.Code);
        });

        builder.Entity<Department>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Departments", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(DepartmentConsts.MaxNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(DepartmentConsts.MaxCodeLength);
            b.Property(x => x.Description).HasMaxLength(DepartmentConsts.MaxDescriptionLength);

            b.HasIndex(x => x.Code);
        });

        builder.Entity<TicketStatus>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketStatuses", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(TicketStatusConsts.MaxNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(TicketStatusConsts.MaxCodeLength);
            b.Property(x => x.Color).HasMaxLength(TicketStatusConsts.MaxColorLength);

            b.HasIndex(x => x.Code);
        });

        builder.Entity<TicketSource>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketSources", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(TicketSourceConsts.MaxNameLength);
            b.Property(x => x.Code).IsRequired().HasMaxLength(TicketSourceConsts.MaxCodeLength);

            b.HasIndex(x => x.Code);
        });

        builder.Entity<CannedResponse>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "CannedResponses", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(CannedResponseConsts.MaxTitleLength);
            b.Property(x => x.Content).IsRequired().HasMaxLength(CannedResponseConsts.MaxContentLength);

            b.HasIndex(x => x.CategoryId);
        });

        builder.Entity<Helpdesk.Tickets.Ticket>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Tickets", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TicketNumber).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxTicketNumberLength);
            b.Property(x => x.Title).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxTitleLength);
            b.Property(x => x.RequesterName).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxRequesterNameLength);
            b.Property(x => x.RequesterEmail).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxRequesterEmailLength);
            b.Property(x => x.RequesterPhone).HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxRequesterPhoneLength);
            b.Property(x => x.Tags).HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxTagsLength);
            b.Property(x => x.CsatComment).HasMaxLength(1000);

            b.HasIndex(x => x.TicketNumber).IsUnique();
            b.HasIndex(x => x.StatusId);
            b.HasIndex(x => x.PriorityId);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.DepartmentId);
            b.HasIndex(x => x.AssigneeId);
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.CsatRating);
        });

        builder.Entity<Helpdesk.Tickets.TicketComment>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketComments", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Content).IsRequired();

            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<Helpdesk.Tickets.TicketAttachment>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketAttachments", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileName).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxAttachmentFileNameLength);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxAttachmentContentTypeLength);
            b.Property(x => x.BlobName).IsRequired();

            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<Helpdesk.Tickets.TicketActivity>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketActivities", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Description).HasMaxLength(Helpdesk.Tickets.TicketConsts.MaxActivityDescriptionLength);

            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<Helpdesk.Sla.SlaPolicy>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "SlaPolicies", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(Helpdesk.Sla.SlaConsts.MaxNameLength);
            b.Property(x => x.Description).HasMaxLength(Helpdesk.Sla.SlaConsts.MaxDescriptionLength);

            b.HasMany(x => x.Rules).WithOne().HasForeignKey(r => r.SlaPolicyId).IsRequired().OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
        });

        builder.Entity<Helpdesk.Sla.SlaPolicyRule>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "SlaPolicyRules", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.SlaPolicyId, x.PriorityId, x.CategoryId });
        });

        builder.Entity<Helpdesk.Sla.BusinessHour>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "BusinessHours", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.DayOfWeek);
        });

        builder.Entity<Helpdesk.Sla.Holiday>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Holidays", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(Helpdesk.Sla.SlaConsts.MaxHolidayNameLength);
            b.HasIndex(x => x.Date);
        });

        builder.Entity<Helpdesk.Sla.SlaBreachLog>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "SlaBreachLogs", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.TicketId);
            b.HasIndex(x => x.BreachedAt);
        });

        builder.Entity<Helpdesk.KnowledgeBase.KnowledgeArticle>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "KnowledgeArticles", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Slug).IsRequired().HasMaxLength(256);
            b.Property(x => x.Summary).HasMaxLength(1000);
            b.Property(x => x.Content).IsRequired();
            b.Property(x => x.Tags).HasMaxLength(500);

            b.HasIndex(x => x.Slug);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.IsPublished);
        });

        builder.Entity<Helpdesk.AssignmentRules.AssignmentRule>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "AssignmentRules", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(Helpdesk.AssignmentRules.AssignmentRuleConsts.MaxNameLength);
            b.Property(x => x.Description).HasMaxLength(Helpdesk.AssignmentRules.AssignmentRuleConsts.MaxDescriptionLength);
            b.Property(x => x.Order).IsRequired().HasDefaultValue(0);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.RoutingStrategy).IsRequired().HasDefaultValue(Helpdesk.AssignmentRules.AssignmentStrategy.RoundRobin);

            b.HasMany(x => x.RuleAgents)
                .WithOne()
                .HasForeignKey(x => x.RuleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.Order);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.PriorityId);
            b.HasIndex(x => x.DepartmentId);
        });

        builder.Entity<Helpdesk.AssignmentRules.AssignmentRuleAgent>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "AssignmentRuleAgents", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.RuleId).IsRequired();
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.Order).IsRequired().HasDefaultValue(0);

            b.HasIndex(x => new { x.RuleId, x.UserId }).IsUnique();
        });
    }
}
