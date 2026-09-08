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
    }
}
