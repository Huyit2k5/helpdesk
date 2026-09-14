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
            b.Property(x => x.DiscordThreadId).HasMaxLength(64);
            b.Property(x => x.AiSummary).HasMaxLength(2000);
            b.Property(x => x.AiSentimentReason).HasMaxLength(500);

            b.HasIndex(x => x.TicketNumber).IsUnique();
            b.HasIndex(x => x.StatusId);
            b.HasIndex(x => x.PriorityId);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.DepartmentId);
            b.HasIndex(x => x.AssigneeId);
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.CsatRating);
            b.HasIndex(x => x.DiscordThreadId);
            b.HasIndex(x => x.AssetId);

            b.HasOne<Helpdesk.Assets.Asset>()
                .WithMany()
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Helpdesk.Tickets.TicketComment>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "TicketComments", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Content).IsRequired();
            b.Property(x => x.AuthorName).HasMaxLength(128);

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

        builder.Entity<Helpdesk.Notifications.Notification>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Notifications", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            b.Property(x => x.Type).IsRequired();
            b.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);

            b.HasIndex(x => x.RecipientUserId);
            b.HasIndex(x => x.IsRead);
            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<Helpdesk.Automations.AutomationRule>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "AutomationRules", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(512);
            b.Property(x => x.TriggerType).IsRequired();
            b.Property(x => x.ExecutionOrder).IsRequired().HasDefaultValue(0);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.StopProcessing).IsRequired().HasDefaultValue(false);
            b.Property(x => x.ConditionsJson).IsRequired();
            b.Property(x => x.ActionsJson).IsRequired();

            b.HasIndex(x => x.TriggerType);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.ExecutionOrder);
        });

        builder.Entity<Helpdesk.Automations.Macro>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Macros", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(512);
            b.Property(x => x.Order).IsRequired().HasDefaultValue(0);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.ActionsJson).IsRequired();

            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<Helpdesk.Assets.Asset>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "Assets", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.AssetTag).IsRequired().HasMaxLength(Helpdesk.Assets.AssetConsts.MaxAssetTagLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(Helpdesk.Assets.AssetConsts.MaxNameLength);
            b.Property(x => x.SerialNumber).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxSerialNumberLength);
            b.Property(x => x.Model).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxModelLength);
            b.Property(x => x.Manufacturer).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxManufacturerLength);
            b.Property(x => x.Location).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxLocationLength);
            b.Property(x => x.AssignedToUserName).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxAssigneeNameLength);
            b.Property(x => x.AssignedToUserEmail).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxAssigneeEmailLength);
            b.Property(x => x.Department).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxDepartmentLength);
            b.Property(x => x.Specifications).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxSpecsLength);
            b.Property(x => x.Notes).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxNotesLength);
            b.Property(x => x.IsHandoverConfirmed).IsRequired().HasDefaultValue(false);
            b.Property(x => x.HandoverConfirmedDate);
            b.Property(x => x.HandoverNotes).HasMaxLength(Helpdesk.Assets.AssetConsts.MaxNotesLength);

            b.Property(x => x.PurchaseCost).HasPrecision(18, 2);
            b.Property(x => x.TotalMaintenanceCost).HasPrecision(18, 2).HasDefaultValue(0);
            b.Property(x => x.LastMaintenanceDate);
            b.Property(x => x.NextMaintenanceDate);
            b.Property(x => x.MaintenanceIntervalMonths);

            b.HasMany(x => x.Activities)
                .WithOne()
                .HasForeignKey(x => x.AssetId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.AssetTag).IsUnique();
            b.HasIndex(x => x.SerialNumber);
            b.HasIndex(x => x.AssetType);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AssignedToUserId);
            b.HasIndex(x => x.Department);
            b.HasIndex(x => x.WarrantyExpiryDate);
            b.HasIndex(x => x.NextMaintenanceDate);
        });

        builder.Entity<Helpdesk.Assets.AssetActivity>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "AssetActivities", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.PerformedByUserName).HasMaxLength(128);

            b.HasIndex(x => x.AssetId);
            b.HasIndex(x => x.ActivityType);
            b.HasIndex(x => x.RelatedTicketId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<Helpdesk.Assets.AssetMaintenance>(b =>
        {
            b.ToTable(HelpdeskConsts.DbTablePrefix + "AssetMaintenances", HelpdeskConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.ServiceProvider).HasMaxLength(256);
            b.Property(x => x.TrackingNumber).HasMaxLength(128);
            b.Property(x => x.ReplacedParts).HasMaxLength(1000);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.PerformedByUserName).HasMaxLength(128);

            b.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            b.Property(x => x.ActualCost).HasPrecision(18, 2);

            b.HasIndex(x => x.AssetId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.MaintenanceType);
            b.HasIndex(x => x.RelatedTicketId);
            b.HasIndex(x => x.StartDate);
        });
    }
}

