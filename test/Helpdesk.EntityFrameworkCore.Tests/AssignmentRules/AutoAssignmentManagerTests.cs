using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.AssignmentRules;
using Helpdesk.Notifications;
using Helpdesk.Priorities;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.AssignmentRules;

public class AutoAssignmentManagerTests : HelpdeskEntityFrameworkCoreTestBase, IDisposable
{
    private readonly List<Guid> _cleanupUserIds = new();
    private readonly List<Guid> _cleanupTicketIds = new();
    private readonly List<Guid> _cleanupRuleIds = new();
    private readonly List<Guid> _cleanupAgentIds = new();

    [Fact]
    public async Task RoundRobin_ShouldCycleThroughAgentsInOrder()
    {
        var agentIds = await CreateUsersAsync(3);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.RoundRobin, agentIds, order: 1);

        // 1st ticket -> agent index 0 (LastAssignedUserId null)
        var first = await RunAssignmentAsync();
        first.AssigneeId.ShouldNotBeNull();
        first.AssigneeId!.Value.ShouldBe(agentIds[0]);

        // 2nd -> agent index 1
        var second = await RunAssignmentAsync();
        second.AssigneeId!.Value.ShouldBe(agentIds[1]);

        // 3rd -> agent index 2
        var third = await RunAssignmentAsync();
        third.AssigneeId!.Value.ShouldBe(agentIds[2]);

        // 4th -> wraps back to index 0
        var fourth = await RunAssignmentAsync();
        fourth.AssigneeId!.Value.ShouldBe(agentIds[0]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task RoundRobin_ShouldPersistLastAssignedUserId()
    {
        var agentIds = await CreateUsersAsync(2);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.RoundRobin, agentIds, order: 1);

        await RunAssignmentAsync(); // assigns agent[0]

        var ruleRepo = GetRequiredService<IRepository<AssignmentRule, Guid>>();
        var rule = await ruleRepo.FindAsync(ruleId);
        rule!.LastAssignedUserId.ShouldNotBeNull();
        rule.LastAssignedUserId!.Value.ShouldBe(agentIds[0]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task RoundRobin_ShouldContinueFromExistingLastAssignedUser()
    {
        var agentIds = await CreateUsersAsync(3);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.RoundRobin, agentIds, order: 1);

        // Pre-set LastAssignedUserId to agent[1] (index 1) -> next should be agent[2]
        var ruleRepo = GetRequiredService<IRepository<AssignmentRule, Guid>>();
        var rule = await ruleRepo.FindAsync(ruleId)!;
        rule.LastAssignedUserId = agentIds[1];
        await ruleRepo.UpdateAsync(rule, autoSave: true);

        var chosen = await RunAssignmentAsync();
        chosen.AssigneeId!.Value.ShouldBe(agentIds[2]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task LeastBusy_ShouldPickAgentWithFewestOpenTickets()
    {
        var agentIds = await CreateUsersAsync(2);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.LeastBusy, agentIds, order: 1);

        // Give agent[1] several open tickets, leave agent[0] with none.
        await SeedOpenTicketsForAsync(agentIds[1], count: 3);

        var chosen = await RunAssignmentAsync();
        chosen.AssigneeId!.Value.ShouldBe(agentIds[0]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task LeastBusy_ShouldIgnoreClosedTickets()
    {
        var agentIds = await CreateUsersAsync(2);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.LeastBusy, agentIds, order: 1);

        // Give agent[1] many CLOSED tickets (should not count toward load).
        await SeedTicketsForAsync(agentIds[1], count: 3, closed: true);

        var chosen = await RunAssignmentAsync();
        // agent[0] has 0 open, agent[1] has 0 open (closed don't count) -> stable first candidate.
        chosen.AssigneeId!.Value.ShouldBe(agentIds[0]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task DirectAssign_ShouldAssignFixedAssignee()
    {
        var agentIds = await CreateUsersAsync(1);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.DirectAssign, agentIds, order: 1, directAssigneeId: agentIds[0]);

        var chosen = await RunAssignmentAsync();
        chosen.AssigneeId!.Value.ShouldBe(agentIds[0]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task AlreadyAssigned_ShouldNotOverride()
    {
        var agentIds = await CreateUsersAsync(2);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.RoundRobin, agentIds, order: 1);

        var ticket = await CreateTicketAsync();
        ticket.AssigneeId = agentIds[1];

        var mgr = GetRequiredService<AutoAssignmentManager>();
        var result = await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));

        result.ShouldBeFalse();
        ticket.AssigneeId!.Value.ShouldBe(agentIds[1]);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task CategoryFilter_Mismatch_ShouldSkipRule()
    {
        var agentIds = await CreateUsersAsync(1);
        // Rule restricted to a specific category that does NOT match the new ticket.
        var categoryRepo = GetRequiredService<Helpdesk.Categories.ICategoryRepository>();
        var categories = await categoryRepo.GetListAsync();
        var someCategory = categories[0].Id;

        var ruleId = await CreateRuleAsync(AssignmentStrategy.DirectAssign, agentIds, order: 1,
            directAssigneeId: agentIds[0], categoryId: someCategory);

        // Ticket with a different category.
        var otherCategory = categories.Skip(1).FirstOrDefault() ?? categories[0];
        var ticket = await CreateTicketAsync(categoryId: otherCategory.Id);
        // Ensure it's not the same category as the rule's filter.
        if (ticket.CategoryId == someCategory)
        {
            ticket.CategoryId = categories.First(c => c.Id != someCategory).Id;
        }

        var mgr = GetRequiredService<AutoAssignmentManager>();
        var result = await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));

        result.ShouldBeFalse();
        ticket.AssigneeId.ShouldBeNull();

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task InactiveRule_ShouldBeSkipped()
    {
        var agentIds = await CreateUsersAsync(1);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.DirectAssign, agentIds, order: 1,
            directAssigneeId: agentIds[0], isActive: false);

        var ticket = await CreateTicketAsync();
        var mgr = GetRequiredService<AutoAssignmentManager>();
        var result = await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));

        result.ShouldBeFalse();
        ticket.AssigneeId.ShouldBeNull();

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task Assign_ShouldCreateActivityAndNotification()
    {
        var agentIds = await CreateUsersAsync(1);
        var ruleId = await CreateRuleAsync(AssignmentStrategy.DirectAssign, agentIds, order: 1, directAssigneeId: agentIds[0]);

        var ticket = await CreateTicketAsync();

        var activityRepo = GetRequiredService<IRepository<TicketActivity, Guid>>();
        var notifRepo = GetRequiredService<IRepository<Notification, Guid>>();
        long activityBefore = await activityRepo.GetCountAsync();
        long notifBefore = await notifRepo.GetCountAsync();

        var mgr = GetRequiredService<AutoAssignmentManager>();
        var result = await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));

        result.ShouldBeTrue();
        ticket.AssigneeId!.Value.ShouldBe(agentIds[0]);

        long activityAfter = await activityRepo.GetCountAsync();
        long notifAfter = await notifRepo.GetCountAsync();
        activityAfter.ShouldBeGreaterThanOrEqualTo(activityBefore + 1);
        notifAfter.ShouldBeGreaterThanOrEqualTo(notifBefore + 1);

        CleanupRule(ruleId);
    }

    [Fact]
    public async Task NoActiveRules_ShouldReturnFalse()
    {
        // Ensure there are no active assignment rules at all.
        var ruleRepo = GetRequiredService<IRepository<AssignmentRule, Guid>>();
        var ticket = await CreateTicketAsync();

        var mgr = GetRequiredService<AutoAssignmentManager>();
        var result = await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));

        result.ShouldBeFalse();
        ticket.AssigneeId.ShouldBeNull();
        _ = ruleRepo;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<Guid[]> CreateUsersAsync(int count)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var userManager = GetRequiredService<Volo.Abp.Identity.IdentityUserManager>();
        var ids = new Guid[count];
        for (int i = 0; i < count; i++)
        {
            var id = guidGen.Create();
            ids[i] = id;
            var user = new IdentityUser(id, $"agent_{Guid.NewGuid():N}", $"agent_{Guid.NewGuid():N}@test.com");
            user.Name = "Agent";
            user.Surname = i.ToString();
            await userManager.CreateAsync(user, "Passw0rd!");
            _cleanupUserIds.Add(id);
        }
        return ids;
    }

    private async Task<Guid> CreateRuleAsync(
        AssignmentStrategy strategy,
        Guid[] agentIds,
        int order = 0,
        Guid? directAssigneeId = null,
        Guid? categoryId = null,
        Guid? priorityId = null,
        Guid? sourceId = null,
        bool isActive = true)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var ruleId = guidGen.Create();
        var rule = new AssignmentRule(
            ruleId,
            $"Rule_{Guid.NewGuid():N}",
            order: order,
            routingStrategy: strategy,
            departmentId: null,
            categoryId: categoryId,
            priorityId: priorityId,
            sourceId: sourceId,
            directAssigneeId: directAssigneeId,
            isActive: isActive);

        for (int i = 0; i < agentIds.Length; i++)
        {
            var agentId = guidGen.Create();
            rule.AddAgent(agentId, agentIds[i], order: i);
            _cleanupAgentIds.Add(agentId);
        }

        var ruleRepo = GetRequiredService<IRepository<AssignmentRule, Guid>>();
        await ruleRepo.InsertAsync(rule, autoSave: true);
        _cleanupRuleIds.Add(ruleId);
        return ruleId;
    }

    private async Task<Ticket> CreateTicketAsync(Guid? categoryId = null)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();

        var categories = await GetRequiredService<Helpdesk.Categories.ICategoryRepository>().GetListAsync();
        var cat = categoryId.HasValue
            ? categories.First(c => c.Id == categoryId)
            : (categories.FirstOrDefault(c => c.Code == "HARDWARE") ?? categories[0]);

        var priorities = await GetRequiredService<IRepository<Priority, Guid>>().GetListAsync();
        var prio = priorities.First(p => p.Code == "MEDIUM");

        var statuses = await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync();
        var newStatus = statuses.First(s => s.Code == "NEW");

        var sources = await GetRequiredService<Helpdesk.TicketSources.ITicketSourceRepository>().GetListAsync();
        var src = sources.First(s => s.Code == "EMAIL");

        var id = guidGen.Create();
        var ticket = new Ticket(
            id,
            $"TK-209903-{Guid.NewGuid():N}".Substring(0, 32),
            "Auto assign test",
            "desc",
            cat.Id, prio.Id, newStatus.Id, src.Id,
            "Requester", "req@test.com");
        await ticketRepo.InsertAsync(ticket, autoSave: true);
        _cleanupTicketIds.Add(id);
        return ticket;
    }

    private async Task<Ticket> RunAssignmentAsync(Guid? categoryId = null)
    {
        var ticket = await CreateTicketAsync(categoryId);
        var mgr = GetRequiredService<AutoAssignmentManager>();
        await WithUnitOfWorkAsync(async () => await mgr.TryAssignTicketAsync(ticket));
        return ticket;
    }

    private async Task SeedOpenTicketsForAsync(Guid userId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await SeedTicketsForAsync(userId, count: 1, closed: false);
        }
    }

    private async Task SeedTicketsForAsync(Guid userId, int count, bool closed)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();
        var categories = await GetRequiredService<Helpdesk.Categories.ICategoryRepository>().GetListAsync();
        var cat = categories[0];
        var priorities = await GetRequiredService<IRepository<Priority, Guid>>().GetListAsync();
        var prio = priorities[0];
        var statuses = await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync();
        var status = closed ? statuses.First(s => s.Code == "RESOLVED") : statuses.First(s => s.Code == "NEW");
        var sources = await GetRequiredService<Helpdesk.TicketSources.ITicketSourceRepository>().GetListAsync();
        var src = sources[0];

        for (int i = 0; i < count; i++)
        {
            var id = guidGen.Create();
            var ticket = new Ticket(
                id,
                $"TK-209904-{Guid.NewGuid():N}".Substring(0, 32),
                "Load ticket",
                "desc",
                cat.Id, prio.Id, status.Id, src.Id,
                "Requester", "req@test.com",
                assigneeId: userId);
            if (closed)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
                ticket.ClosedAt = DateTime.UtcNow;
            }
            await ticketRepo.InsertAsync(ticket, autoSave: true);
            _cleanupTicketIds.Add(id);
        }
    }

    private void CleanupRule(Guid ruleId)
    {
        try
        {
            var agentRepo = GetRequiredService<IRepository<AssignmentRuleAgent, Guid>>();
            var agents = agentRepo.GetListAsync().GetAwaiter().GetResult();
            foreach (var a in agents.Where(a => a.RuleId == ruleId))
            {
                agentRepo.DeleteAsync(a, autoSave: true).GetAwaiter().GetResult();
            }
            var ruleRepo = GetRequiredService<IRepository<AssignmentRule, Guid>>();
            var rule = ruleRepo.FindAsync(ruleId).GetAwaiter().GetResult();
            if (rule != null)
            {
                ruleRepo.DeleteAsync(rule, autoSave: true).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }

    public override void Dispose()
    {
        // Users/agents/rule rows are per-test (unique GUIDs, unique ticket numbers) and the
        // collection DB is fresh per test class, so nothing needs to be cleaned up globally.
        base.Dispose();
    }
}
