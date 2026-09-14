using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Tickets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Tickets;

public class TicketNumberRaceConditionTests : HelpdeskEntityFrameworkCoreTestBase, IDisposable
{
    // Fixed timestamp in a month with no seeded tickets, so the generated sequence is
    // deterministic (the seeded sample tickets use TK-<current month>-0001..0010).
    private static readonly DateTime _fixedNow = new DateTime(2099, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private const string _prefix = "TK-209901-";

    [Fact]
    public async Task GenerateTicketNumber_ShouldStartAtFirstSequenceForNewMonth()
    {
        using var scope = CreateFixedClockScope();
        var mgr = BuildTicketManager(scope.ServiceProvider);
        var number = await WithUnitOfWorkAsync(async () => await mgr.GenerateTicketNumberAsync());

        number.ShouldStartWith(_prefix);
        number.ShouldBe($"{_prefix}0001");
    }

    [Fact]
    public async Task GenerateTicketNumber_ShouldIncrementWhenTicketsExist()
    {
        // Seed three tickets with the fixed month's prefix.
        await SeedTicketAsync($"{_prefix}0001");
        await SeedTicketAsync($"{_prefix}0002");
        await SeedTicketAsync($"{_prefix}0003");

        using var scope = CreateFixedClockScope();
        var mgr = BuildTicketManager(scope.ServiceProvider);
        var number = await WithUnitOfWorkAsync(async () => await mgr.GenerateTicketNumberAsync());

        number.ShouldBe($"{_prefix}0004");
    }

    [Fact]
    public async Task GenerateTicketNumber_ShouldCountSoftDeletedTickets()
    {
        // Seed one live + one soft-deleted ticket with consecutive numbers.
        await SeedTicketAsync($"{_prefix}0001");
        var deleted = await SeedTicketAsync($"{_prefix}0002");

        // Soft-delete the second one.
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();
        await WithUnitOfWorkAsync(async () => await ticketRepo.DeleteAsync(deleted, autoSave: true));

        // The soft-deleted ticket must still be counted, otherwise the next number
        // would collide with 0002 (unique constraint IX_AppTickets_TicketNumber).
        using var scope = CreateFixedClockScope();
        var mgr = BuildTicketManager(scope.ServiceProvider);
        var number = await WithUnitOfWorkAsync(async () => await mgr.GenerateTicketNumberAsync());

        number.ShouldBe($"{_prefix}0003");
    }

    [Fact]
    public async Task GenerateTicketNumber_ShouldNotReuseASoftDeletedNumber()
    {
        // Seed 0001 live and 0002 soft-deleted, leaving a gap.
        await SeedTicketAsync($"{_prefix}0001");
        var deleted = await SeedTicketAsync($"{_prefix}0002");
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();
        await WithUnitOfWorkAsync(async () => await ticketRepo.DeleteAsync(deleted, autoSave: true));

        // Next candidate should skip the soft-deleted 0002.
        using var scope = CreateFixedClockScope();
        var mgr = BuildTicketManager(scope.ServiceProvider);
        var number = await WithUnitOfWorkAsync(async () => await mgr.GenerateTicketNumberAsync());

        number.ShouldNotBe($"{_prefix}0002");
        number.ShouldBe($"{_prefix}0003");
    }

    [Fact]
    public async Task GenerateTicketNumber_ShouldNotCollideWithExistingNumber()
    {
        // Simulates the race scenario: a ticket with number 0001 exists, and the generator
        // must not return 0001 (it would violate the unique constraint IX_AppTickets_TicketNumber).
        // The current implementation uses Count() + while(Any(candidate)) which is NOT atomic,
        // so two concurrent calls could both compute 0002. This deterministic test verifies
        // that a single call at least does not collide with an existing number.
        await SeedTicketAsync($"{_prefix}0001");

        using var scope = CreateFixedClockScope();
        var mgr = BuildTicketManager(scope.ServiceProvider);
        var number = await WithUnitOfWorkAsync(async () => await mgr.GenerateTicketNumberAsync());

        // Must not reuse 0001 (already exists).
        number.ShouldNotBe($"{_prefix}0001");
        number.ShouldBe($"{_prefix}0002");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private Microsoft.Extensions.DependencyInjection.IServiceScope CreateFixedClockScope()
    {
        return ServiceProvider.CreateScope();
    }

    private TicketManager BuildTicketManager(IServiceProvider sp)
    {
        // Build a TicketManager that uses a FixedClock so the ticket-number prefix is
        // deterministic (TK-209901-###).
        var fixedClock = new FixedClock(
            sp.GetRequiredService<IOptions<AbpClockOptions>>(),
            sp.GetRequiredService<ICurrentTimezoneProvider>(),
            sp.GetRequiredService<ITimezoneProvider>());
        return new TicketManager(
            sp.GetRequiredService<IRepository<Ticket, Guid>>(),
            sp.GetRequiredService<IRepository<TicketActivity, Guid>>(),
            fixedClock,
            sp.GetRequiredService<Volo.Abp.Data.IDataFilter>());
    }

    private async Task<Ticket> SeedTicketAsync(string number)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();
        var categories = await GetRequiredService<Helpdesk.Categories.ICategoryRepository>().GetListAsync();
        var cat = categories[0];
        var priorities = await GetRequiredService<IRepository<Helpdesk.Priorities.Priority, Guid>>().GetListAsync();
        var prio = priorities[0];
        var statuses = await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync();
        var status = statuses.First(s => s.Code == "NEW");
        var sources = await GetRequiredService<Helpdesk.TicketSources.ITicketSourceRepository>().GetListAsync();
        var src = sources[0];

        var ticket = new Ticket(
            guidGen.Create(),
            number,
            "Race test ticket",
            "desc",
            cat.Id, prio.Id, status.Id, src.Id,
            "Requester", "req@test.com");

        await WithUnitOfWorkAsync(async () => await ticketRepo.InsertAsync(ticket, autoSave: true));
        return ticket;
    }

    public override void Dispose()
    {
        // No cross-test cleanup needed: ticket numbers are derived from a fixed month
        // (209901) and each seeded row uses a unique Guid; the collection DB is fresh
        // per test class.
        base.Dispose();
    }

    private sealed class FixedClock : Clock
    {
        private readonly AbpClockOptions _options;
        private readonly ICurrentTimezoneProvider _currentTimezoneProvider;
        private readonly ITimezoneProvider _timezoneProvider;

        public FixedClock(
            IOptions<AbpClockOptions> options,
            ICurrentTimezoneProvider currentTimezoneProvider,
            ITimezoneProvider timezoneProvider)
            : base(options, currentTimezoneProvider, timezoneProvider)
        {
            _options = options.Value;
            _currentTimezoneProvider = currentTimezoneProvider;
            _timezoneProvider = timezoneProvider;
        }

        public override DateTime Now => _fixedNow;
    }
}
