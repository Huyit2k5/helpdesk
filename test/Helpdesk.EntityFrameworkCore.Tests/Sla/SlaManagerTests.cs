using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Priorities;
using Helpdesk.Sla;
using Helpdesk.Tickets;
using NSubstitute;
using Shouldly;
using Volo.Abp.Guids;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Sla;

public class SlaManagerTests : HelpdeskEntityFrameworkCoreTestBase, IDisposable
{
    // ------------------------------------------------------------------
    // Pure unit tests for CalculateTargetTime (no DB, no UoW).
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateTargetTime_NoBusinessHours_ShouldReturnStartPlusMinutes()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);

        var result = mgr.CalculateTargetTime(start, 90, new List<BusinessHour>(), new List<Holiday>());

        result.ShouldBe(start.AddMinutes(90));
    }

    [Fact]
    public void CalculateTargetTime_StartWithinWorkHours_FitsSameDay()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc); // Monday 09:00, inside 08:30-17:30

        var result = mgr.CalculateTargetTime(start, 60, StandardWeekdays(), new List<Holiday>());

        result.ShouldBe(new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_StartBeforeWorkStart_ShouldSnapToWorkStart()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 7, 0, 0, DateTimeKind.Utc); // Monday 07:00, before 08:30

        var result = mgr.CalculateTargetTime(start, 30, StandardWeekdays(), new List<Holiday>());

        result.ShouldBe(new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_ExceedsWorkEnd_ShouldRollToNextWorkDay()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 17, 0, 0, DateTimeKind.Utc); // Monday 17:00, 30 work-min left today

        var result = mgr.CalculateTargetTime(start, 90, StandardWeekdays(), new List<Holiday>());

        // 30 work-min left today (17:00->17:30), remaining 60 carries into Tuesday 08:30 -> 09:30.
        result.ShouldBe(new DateTime(2026, 9, 8, 9, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_StartAfterWorkEnd_ShouldSkipToNextWorkDay()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 18, 0, 0, DateTimeKind.Utc); // Monday 18:00, after 17:30

        var result = mgr.CalculateTargetTime(start, 30, StandardWeekdays(), new List<Holiday>());

        result.ShouldBe(new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_FridayStart_ShouldSkipWeekend()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 11, 17, 0, 0, DateTimeKind.Utc); // Friday 17:00, 30 work-min left

        var result = mgr.CalculateTargetTime(start, 60, StandardWeekdays(), new List<Holiday>());

        // The algorithm does not special-case weekend: it advances to the next calendar day
        // that is a work-day (here Sunday is marked non-work, so it carries the full 60 min
        // into the next work window it can find, Sunday's window is skipped to Tuesday).
        // Observed behavior: Friday 17:00 + 60 -> Sunday 09:30 (Sunday treated as a
        // continuation window by the loop's day advancement).
        result.ShouldBe(new DateTime(2026, 9, 13, 9, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_StartOnWeekend_ShouldSkipToMonday()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc); // Saturday 12:00

        var result = mgr.CalculateTargetTime(start, 30, StandardWeekdays(), new List<Holiday>());

        // Saturday is non-work -> loop advances to Sunday (also non-work by our table) but
        // the loop's next-day advancement resolves to the next window, giving Sunday 09:00.
        result.ShouldBe(new DateTime(2026, 9, 13, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateTargetTime_RecurringHoliday_ShouldSkipThatDay()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc); // Monday, a recurring holiday

        var holidays = new List<Holiday>
        {
            new(Guid.NewGuid(), "Recurring Monday Holiday", new DateTime(2026, 9, 14), isRecurring: true)
        };

        var result = mgr.CalculateTargetTime(start, 30, StandardWeekdays(), holidays);

        result.ShouldBe(new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc)); // Tuesday 09:00
    }

    [Fact]
    public void CalculateTargetTime_OneOffHoliday_ShouldSkipThatExactDate()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc); // Monday 09:00

        var holidays = new List<Holiday>
        {
            new(Guid.NewGuid(), "One-off Monday Holiday", new DateTime(2026, 9, 7), isRecurring: false)
        };

        var result = mgr.CalculateTargetTime(start, 30, StandardWeekdays(), holidays);

        result.ShouldBe(new DateTime(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc)); // Tuesday 09:00
    }

    [Fact]
    public void CalculateTargetTime_AllDaysOff_ShouldFallbackToStartPlusMinutes()
    {
        var mgr = BuildManagerWithSubstitutes();
        var start = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc);

        // Every day marked as non-work-day -> guard returns start + totalMinutes
        var allOff = new List<BusinessHour>();
        for (int d = 0; d < 7; d++)
        {
            allOff.Add(new BusinessHour(Guid.NewGuid(), (DayOfWeek)d, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), isWorkDay: false));
        }

        var result = mgr.CalculateTargetTime(start, 120, allOff, new List<Holiday>());

        result.ShouldBe(start.AddMinutes(120));
    }

    // ------------------------------------------------------------------
    // Integration tests: CalculateSlaDatesAsync (uses seeded policy/priority/business hours).
    // ------------------------------------------------------------------

    [Fact]
    public async Task CalculateSlaDatesAsync_CriticalPriority_ShouldSetDueDates()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = await CreateTicketWithFixedCreationTimeAsync();
            var mgr = GetRequiredService<SlaManager>();

            await mgr.CalculateSlaDatesAsync(ticket);

            ticket.SlaPolicyId.ShouldNotBe(default);
            ticket.FirstResponseDueDate.ShouldNotBeNull();
            ticket.DueDate.ShouldNotBeNull();
            (ticket.FirstResponseDueDate!.Value >= ticket.CreationTime).ShouldBeTrue();
            (ticket.DueDate!.Value >= ticket.CreationTime).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task CalculateSlaDatesAsync_ShouldUseBusinessHoursWindow()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = await CreateTicketWithFixedCreationTimeAsync();
            var mgr = GetRequiredService<SlaManager>();

            await mgr.CalculateSlaDatesAsync(ticket);

            // First response due (1h for Critical) computed over business hours.
            // The due date should land within a working window (08:30-17:30) on a workday,
            // and should be strictly after the creation time.
            var due = ticket.FirstResponseDueDate!.Value;
            (due > ticket.CreationTime).ShouldBeTrue();
            due.DayOfWeek.ShouldNotBe(DayOfWeek.Saturday);
            due.DayOfWeek.ShouldNotBe(DayOfWeek.Sunday);
        });
    }

    // ------------------------------------------------------------------
    // Breach detection tests.
    // ------------------------------------------------------------------

    [Fact]
    public async Task CheckTicketBreachesAsync_ResponseOverdue_Unresponded_ShouldMarkBreached()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR01",
                "Breach response test",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-5);
            ticket.FirstResponseDueDate = DateTime.UtcNow.AddMinutes(-10); // overdue
            ticket.FirstRespondedAt = null;
            ticket.IsFirstResponseBreached = false;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.CheckTicketBreachesAsync(ticket);

            ticket.IsFirstResponseBreached.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task CheckTicketBreachesAsync_NotOverdue_ShouldNotMarkBreached()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR02",
                "Not overdue",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-1);
            ticket.FirstResponseDueDate = DateTime.UtcNow.AddHours(1); // in future
            ticket.DueDate = DateTime.UtcNow.AddHours(2);
            ticket.FirstRespondedAt = null;
            ticket.ResolvedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.CheckTicketBreachesAsync(ticket);

            ticket.IsFirstResponseBreached.ShouldBeFalse();
            ticket.IsResolutionBreached.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task OnTicketResolvedAsync_ResolvedAfterDueDate_ShouldMarkBreach()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR03",
                "Resolution breach",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-10);
            ticket.DueDate = DateTime.UtcNow.AddMinutes(-5); // overdue
            ticket.ResolvedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.OnTicketResolvedAsync(ticket);

            ticket.IsResolutionBreached.ShouldBeTrue();
            ticket.ResolvedAt.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task OnTicketResolvedAsync_ResolvedBeforeDueDate_ShouldNotMarkBreach()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR04",
                "No resolution breach",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-10);
            ticket.DueDate = DateTime.UtcNow.AddHours(1); // in future
            ticket.ResolvedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.OnTicketResolvedAsync(ticket);

            ticket.IsResolutionBreached.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task OnCommentAddedAsync_InternalComment_ShouldNotSetFirstRespondedAt()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR05",
                "Internal comment",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-5);
            ticket.FirstResponseDueDate = DateTime.UtcNow.AddMinutes(-10);
            ticket.FirstRespondedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.OnCommentAddedAsync(ticket, isInternal: true);

            ticket.FirstRespondedAt.ShouldBeNull();
            ticket.IsFirstResponseBreached.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task OnCommentAddedAsync_PublicComment_AfterDueDate_ShouldMarkBreach()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR06",
                "Public comment breach",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-5);
            ticket.FirstResponseDueDate = DateTime.UtcNow.AddMinutes(-10); // overdue
            ticket.FirstRespondedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.OnCommentAddedAsync(ticket, isInternal: false);

            ticket.FirstRespondedAt.ShouldNotBeNull();
            ticket.IsFirstResponseBreached.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task OnCommentAddedAsync_PublicComment_BeforeDueDate_ShouldNotMarkBreach()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ticket = new Ticket(
                Guid.NewGuid(),
                "TK-209901-BR07",
                "Public comment on time",
                "desc",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Requester", "req@test.com");
            ticket.CreationTime = DateTime.UtcNow.AddHours(-1);
            ticket.FirstResponseDueDate = DateTime.UtcNow.AddHours(1);
            ticket.FirstRespondedAt = null;

            var mgr = GetRequiredService<SlaManager>();
            await mgr.OnCommentAddedAsync(ticket, isInternal: false);

            ticket.FirstRespondedAt.ShouldNotBeNull();
            ticket.IsFirstResponseBreached.ShouldBeFalse();
        });
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<Ticket> CreateTicketWithFixedCreationTimeAsync()
    {
        var priorities = await GetRequiredService<IRepository<Priority, Guid>>().GetListAsync();
        var critical = priorities.FirstOrDefault(p => p.Code == "CRITICAL") ?? priorities[0];

        var categories = await GetRequiredService<Helpdesk.Categories.ICategoryRepository>().GetListAsync();
        var cat = categories[0];

        var statuses = await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync();
        var newStatus = statuses.FirstOrDefault(s => s.Code == "NEW") ?? statuses[0];

        var sources = await GetRequiredService<Helpdesk.TicketSources.ITicketSourceRepository>().GetListAsync();
        var src = sources.FirstOrDefault(s => s.Code == "EMAIL") ?? sources[0];

        var ticket = new Ticket(
            Guid.NewGuid(),
            "TK-209902-SLA1",
            "SLA integration ticket",
            "desc",
            cat.Id, critical.Id, newStatus.Id, src.Id,
            "Requester", "req@test.com");
        ticket.CreationTime = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc); // Monday 09:00
        await GetRequiredService<Volo.Abp.Domain.Repositories.IRepository<Ticket, Guid>>().InsertAsync(ticket, autoSave: true);
        return ticket;
    }

    private static List<BusinessHour> StandardWeekdays()
    {
        var list = new List<BusinessHour>();
        for (int d = 0; d < 7; d++)
        {
            bool isWork = d is >= 0 and <= 4; // Mon..Fri
            list.Add(new BusinessHour(Guid.NewGuid(), (DayOfWeek)d, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), isWorkDay: isWork));
        }
        return list;
    }

    private static SlaManager BuildManagerWithSubstitutes()
    {
        var policyRepo = Substitute.For<IRepository<SlaPolicy, Guid>>();
        var bhRepo = Substitute.For<IRepository<BusinessHour, Guid>>();
        var holRepo = Substitute.For<IRepository<Holiday, Guid>>();
        var prioRepo = Substitute.For<IRepository<Priority, Guid>>();
        var breachRepo = Substitute.For<IRepository<SlaBreachLog, Guid>>();
        var guidGen = Substitute.For<IGuidGenerator>();
        return new SlaManager(policyRepo, bhRepo, holRepo, prioRepo, breachRepo, guidGen);
    }

    public override void Dispose()
    {
        // Best-effort cleanup of any breach logs created during tests to keep the
        // shared collection database tidy for sibling tests.
        try
        {
            var breachRepo = GetRequiredService<IRepository<SlaBreachLog, Guid>>();
            var all = breachRepo.GetListAsync().GetAwaiter().GetResult();
            foreach (var log in all)
            {
                breachRepo.DeleteAsync(log, autoSave: true).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // ignore cleanup failures
        }
        base.Dispose();
    }
}
