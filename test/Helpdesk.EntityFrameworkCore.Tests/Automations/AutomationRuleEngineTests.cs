using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Automations;
using Helpdesk.Tickets;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Automations;

public class AutomationRuleEngineTests : HelpdeskEntityFrameworkCoreTestBase, IDisposable
{
    private readonly List<Guid> _cleanupRuleIds = new();

    // ------------------------------------------------------------------
    // Condition evaluation (via ExecuteTriggersAsync with an AddTags action marker).
    // ------------------------------------------------------------------

    [Fact]
    public async Task NoConditions_ShouldAlwaysFire()
    {
        var ticket = await CreateTicketAsync();
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("NOCOND") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags.ShouldNotBeNull();
        ticket.Tags!.Contains("NOCOND", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task TitleContains_Match_ShouldFire()
    {
        var ticket = await CreateTicketAsync(title: "Server crashed sập server now");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Title, Operator = ConditionOperator.Contains, Value = "sập server" } },
            actions: new List<RuleAction> { Tag("KWORD") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("KWORD", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task TitleContains_NoMatch_ShouldNotFire()
    {
        var ticket = await CreateTicketAsync(title: "Routine maintenance request");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Title, Operator = ConditionOperator.Contains, Value = "sập server" } },
            actions: new List<RuleAction> { Tag("KWORD") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        (ticket.Tags?.Contains("KWORD", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task TitleContains_CaseInsensitive_ShouldFire()
    {
        var ticket = await CreateTicketAsync(title: "SERVER SẬP SERVER emergency");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Title, Operator = ConditionOperator.Contains, Value = "sập server" } },
            actions: new List<RuleAction> { Tag("KWORD") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("KWORD", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task StatusEquals_Match_ShouldFire()
    {
        var newStatus = await FindStatusAsync("NEW");
        var ticket = await CreateTicketAsync(statusCode: "NEW");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Status, Operator = ConditionOperator.Equals, Value = newStatus.Id.ToString() } },
            actions: new List<RuleAction> { Tag("STAT") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("STAT", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task StatusNotEquals_Match_ShouldFire()
    {
        var resolvedStatus = await FindStatusAsync("RESOLVED");
        var ticket = await CreateTicketAsync(statusCode: "NEW");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Status, Operator = ConditionOperator.NotEquals, Value = resolvedStatus.Id.ToString() } },
            actions: new List<RuleAction> { Tag("NEQ") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("NEQ", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task PriorityEquals_Match_ShouldFire()
    {
        var prio = (await GetRequiredService<IRepository<Helpdesk.Priorities.Priority, Guid>>().GetListAsync())
            .First(p => p.Code == "CRITICAL");
        var ticket = await CreateTicketAsync(priorityCode: "CRITICAL");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Priority, Operator = ConditionOperator.Equals, Value = prio.Id.ToString() } },
            actions: new List<RuleAction> { Tag("PRIO") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("PRIO", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task TagsContains_Match_ShouldFire()
    {
        var ticket = await CreateTicketAsync(tags: "wifi, network");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Tags, Operator = ConditionOperator.Contains, Value = "wifi" } },
            actions: new List<RuleAction> { Tag("TAGS") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("TAGS", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task AssigneeIsNotEmpty_Match_ShouldFire()
    {
        var userId = await CreateUserAsync();
        var ticket = await CreateTicketAsync();
        ticket.AssigneeId = userId;
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Assignee, Operator = ConditionOperator.IsNotEmpty } },
            actions: new List<RuleAction> { Tag("ASG") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("ASG", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task IsUnassigned_True_ShouldFireForUnassigned()
    {
        var ticket = await CreateTicketAsync(); // no assignee
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.IsUnassigned, Operator = ConditionOperator.Equals, Value = "true" } },
            actions: new List<RuleAction> { Tag("UNASG") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("UNASG", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task IsUnassigned_True_ShouldNotFireForAssigned()
    {
        var userId = await CreateUserAsync();
        var ticket = await CreateTicketAsync();
        ticket.AssigneeId = userId;
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.IsUnassigned, Operator = ConditionOperator.Equals, Value = "true" } },
            actions: new List<RuleAction> { Tag("UNASG") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        (ticket.Tags?.Contains("UNASG", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task HoursSinceCreated_GreaterThan_ShouldFireWhenOld()
    {
        var ticket = await CreateTicketAsync();
        ticket.CreationTime = DateTime.UtcNow.AddHours(-100);
        ticket.LastModificationTime = DateTime.UtcNow.AddHours(-100);
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.HoursSinceCreated, Operator = ConditionOperator.GreaterThan, Value = "48" } },
            actions: new List<RuleAction> { Tag("OLD") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("OLD", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task HoursSinceCreated_LessThan_ShouldFireWhenNew()
    {
        var ticket = await CreateTicketAsync(); // created "now"
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.HoursSinceCreated, Operator = ConditionOperator.LessThan, Value = "1" } },
            actions: new List<RuleAction> { Tag("NEW") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("NEW", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task MultipleConditions_And_AllMatch_ShouldFire()
    {
        var newStatus = await FindStatusAsync("NEW");
        var ticket = await CreateTicketAsync(title: "sập server critical", statusCode: "NEW");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>
            {
                new() { Field = ConditionField.Title, Operator = ConditionOperator.Contains, Value = "sập server" },
                new() { Field = ConditionField.Status, Operator = ConditionOperator.Equals, Value = newStatus.Id.ToString() }
            },
            actions: new List<RuleAction> { Tag("AND") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("AND", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task MultipleConditions_OneFails_ShouldNotFire()
    {
        var ticket = await CreateTicketAsync(title: "nothing to see here");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>
            {
                new() { Field = ConditionField.Title, Operator = ConditionOperator.Contains, Value = "sập server" }, // false
                new() { Field = ConditionField.Tags, Operator = ConditionOperator.Contains, Value = "wifi" } // false
            },
            actions: new List<RuleAction> { Tag("AND") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        (ticket.Tags?.Contains("AND", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task MalformedConditionsJson_ShouldTreatAsNoConditionAndFire()
    {
        var ticket = await CreateTicketAsync();
        var rule = BuildRule(AutomationTriggerType.OnTicketCreated);
        rule.ConditionsJson = "{ not valid json";
        rule.SetActions(new List<RuleAction> { Tag("BADJSON") });
        await InsertRawRuleAsync(rule);

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("BADJSON", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task MalformedActionsJson_ShouldNotThrow()
    {
        var ticket = await CreateTicketAsync();
        var rule = BuildRule(AutomationTriggerType.OnTicketCreated);
        rule.ActionsJson = "{ not valid json";
        await InsertRawRuleAsync(rule);

        var engine = GetRequiredService<AutomationRuleEngine>();
        await WithUnitOfWorkAsync(async () => await engine.ExecuteTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated));

        // Should not throw; audit activity still written.
    }

    // ------------------------------------------------------------------
    // Action execution.
    // ------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_Action_ShouldChangeStatus()
    {
        var inProgress = await FindStatusAsync("IN_PROGRESS");
        var ticket = await CreateTicketAsync(statusCode: "NEW");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.ChangeStatus, TargetValue = inProgress.Id.ToString() } });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.StatusId.ShouldBe(inProgress.Id);
    }

    [Fact]
    public async Task ChangePriority_Action_ShouldChangePriority()
    {
        var critical = (await GetRequiredService<IRepository<Helpdesk.Priorities.Priority, Guid>>().GetListAsync())
            .First(p => p.Code == "CRITICAL");
        var ticket = await CreateTicketAsync(priorityCode: "LOW");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.ChangePriority, TargetValue = critical.Id.ToString() } });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.PriorityId.ShouldBe(critical.Id);
    }

    [Fact]
    public async Task AssignToUser_Action_ShouldAssign()
    {
        var userId = await CreateUserAsync();
        var ticket = await CreateTicketAsync();
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.AssignToUser, TargetValue = userId.ToString() } });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.AssigneeId.ShouldBe(userId);
    }

    [Fact]
    public async Task AssignToDepartment_Action_ShouldAssignDepartment()
    {
        var dept = (await GetRequiredService<IRepository<Helpdesk.Departments.Department, Guid>>().GetListAsync())[0];
        var ticket = await CreateTicketAsync();
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.AssignToDepartment, TargetValue = dept.Id.ToString() } });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.DepartmentId.ShouldBe(dept.Id);
    }

    [Fact]
    public async Task AddTags_Action_ShouldAddAndNotDuplicate()
    {
        var ticket = await CreateTicketAsync(tags: "existing");
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("added"), Tag("existing") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("added", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        // "existing" should not be duplicated.
        var existingCount = ticket.Tags!.Split(',').Count(t => t.Trim().Equals("existing", StringComparison.OrdinalIgnoreCase));
        existingCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddComment_Action_ShouldCreateComment()
    {
        var ticket = await CreateTicketAsync();
        var commentRepo = GetRequiredService<IRepository<TicketComment, Guid>>();
        long before = await commentRepo.GetCountAsync();

        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.AddComment, TargetValue = "Auto note", AdditionalValue = "true" } });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        long after = await commentRepo.GetCountAsync();
        after.ShouldBeGreaterThanOrEqualTo(before + 1);
    }

    [Fact]
    public async Task SendDiscordAlert_Action_ShouldInvokeDiscordService()
    {
        var discord = Substitute.For<Helpdesk.Discord.IDiscordNotificationService>();
        discord.SendTicketCreatedAsync(Arg.Any<Ticket>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        var ticket = await CreateTicketAsync();
        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { new() { ActionType = AutomationActionType.SendDiscordAlert, TargetValue = "alert" } });

        // Resolve the engine from DI (fully wired, ambient service provider set) and swap
        // only the private Discord field with a substitute so we can assert on it.
        AutomationRuleEngine engine;
        await WithUnitOfWorkAsync(async () =>
        {
            using var scope = ServiceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            engine = sp.GetRequiredService<AutomationRuleEngine>();
            typeof(AutomationRuleEngine)
                .GetField("_discordNotificationService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(engine, discord);
            await engine.ExecuteTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);
        });

        await discord.Received(1).SendTicketCreatedAsync(Arg.Any<Ticket>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task EachExecutedRule_ShouldWriteAutomationExecutedActivity()
    {
        var ticket = await CreateTicketAsync();
        var activityRepo = GetRequiredService<IRepository<TicketActivity, Guid>>();
        long before = await activityRepo.GetCountAsync();

        await InsertRuleAsync(triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("A1") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        long after = await activityRepo.GetCountAsync();
        after.ShouldBeGreaterThanOrEqualTo(before + 1);
    }

    // ------------------------------------------------------------------
    // Orchestration.
    // ------------------------------------------------------------------

    [Fact]
    public async Task StopProcessing_ShouldPreventLaterRules()
    {
        var ticket = await CreateTicketAsync();
        await InsertRuleAsync(order: 1, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("FIRST") }, stopProcessing: true);
        await InsertRuleAsync(order: 2, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("SECOND") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        ticket.Tags!.Contains("FIRST", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        (ticket.Tags?.Contains("SECOND", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task RulesExecuteInExecutionOrder()
    {
        var ticket = await CreateTicketAsync();
        // Insert out of order; order is controlled by ExecutionOrder.
        await InsertRuleAsync(order: 2, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("SECOND") });
        await InsertRuleAsync(order: 1, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("FIRST") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        var tags = ticket.Tags!.Split(',').Select(t => t.Trim()).ToList();
        var firstIdx = tags.FindIndex(t => string.Equals(t, "FIRST", StringComparison.OrdinalIgnoreCase));
        var secondIdx = tags.FindIndex(t => string.Equals(t, "SECOND", StringComparison.OrdinalIgnoreCase));
        firstIdx.ShouldBeGreaterThan(-1);
        secondIdx.ShouldBeGreaterThan(-1);
        firstIdx.ShouldBeLessThan(secondIdx);
    }

    [Fact]
    public async Task NonMatchingRule_ShouldNotBlockMatchingRule()
    {
        var newStatus = await FindStatusAsync("NEW");
        var ticket = await CreateTicketAsync(statusCode: "NEW");
        // Rule order 1 won't match (wrong status), rule order 2 always matches.
        await InsertRuleAsync(order: 1, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition> { new() { Field = ConditionField.Status, Operator = ConditionOperator.Equals, Value = Guid.NewGuid().ToString() } },
            actions: new List<RuleAction> { Tag("R1") });
        await InsertRuleAsync(order: 2, triggers: AutomationTriggerType.OnTicketCreated,
            conditions: new List<RuleCondition>(),
            actions: new List<RuleAction> { Tag("R2") });

        await RunTriggersAsync(ticket, AutomationTriggerType.OnTicketCreated);

        (ticket.Tags?.Contains("R1", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
        ticket.Tags!.Contains("R2", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        _ = newStatus;
    }

    [Fact]
    public async Task ApplyMacro_Active_ShouldApplyActions()
    {
        var inProgress = await FindStatusAsync("IN_PROGRESS");
        var ticket = await CreateTicketAsync(statusCode: "NEW");
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var macroId = guidGen.Create();
        var macro = new Macro(macroId, $"Macro_{Guid.NewGuid():N}", isActive: true);
        macro.SetActions(new List<RuleAction>
        {
            new() { ActionType = AutomationActionType.ChangeStatus, TargetValue = inProgress.Id.ToString() },
            Tag("MACRO")
        });
        await GetRequiredService<IRepository<Macro, Guid>>().InsertAsync(macro, autoSave: true);

        var engine = GetRequiredService<AutomationRuleEngine>();
        var result = await WithUnitOfWorkAsync(async () => await engine.ApplyMacroAsync(ticket, macroId));

        result.ShouldBeTrue();
        ticket.StatusId.ShouldBe(inProgress.Id);
        ticket.Tags!.Contains("MACRO", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyMacro_Inactive_ShouldReturnFalse()
    {
        var ticket = await CreateTicketAsync();
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var macroId = guidGen.Create();
        var macro = new Macro(macroId, $"MacroOff_{Guid.NewGuid():N}", isActive: false);
        macro.SetActions(new List<RuleAction> { Tag("MACRO") });
        await GetRequiredService<IRepository<Macro, Guid>>().InsertAsync(macro, autoSave: true);

        var engine = GetRequiredService<AutomationRuleEngine>();
        var result = await WithUnitOfWorkAsync(async () => await engine.ApplyMacroAsync(ticket, macroId));

        result.ShouldBeFalse();
        (ticket.Tags?.Contains("MACRO", StringComparison.OrdinalIgnoreCase) ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task NoActiveRulesForTrigger_ShouldReturnFalse()
    {
        var ticket = await CreateTicketAsync();
        var engine = GetRequiredService<AutomationRuleEngine>();
        var result = await WithUnitOfWorkAsync(async () => await engine.ExecuteTriggersAsync(ticket, AutomationTriggerType.OnCommentAdded));

        result.ShouldBeFalse();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<Helpdesk.TicketStatuses.TicketStatus> FindStatusAsync(string code)
    {
        return (await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync())
            .First(s => s.Code == code);
    }

    private async Task<Guid> CreateUserAsync()
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var id = guidGen.Create();
        var user = new IdentityUser(id, $"auto_{Guid.NewGuid():N}", $"auto_{Guid.NewGuid():N}@test.com");
        user.Name = "Auto";
        user.Surname = "User";
        await GetRequiredService<Volo.Abp.Identity.IdentityUserManager>().CreateAsync(user, "Passw0rd!");
        return id;
    }

    private async Task<Ticket> CreateTicketAsync(string title = "Auto test", string? tags = null, string statusCode = "NEW", string priorityCode = "MEDIUM")
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var ticketRepo = GetRequiredService<IRepository<Ticket, Guid>>();
        var categories = await GetRequiredService<Helpdesk.Categories.ICategoryRepository>().GetListAsync();
        var cat = categories[0];
        var priorities = await GetRequiredService<IRepository<Helpdesk.Priorities.Priority, Guid>>().GetListAsync();
        var prio = priorities.First(p => p.Code == priorityCode);
        var statuses = await GetRequiredService<Helpdesk.TicketStatuses.ITicketStatusRepository>().GetListAsync();
        var status = statuses.First(s => s.Code == statusCode);
        var sources = await GetRequiredService<Helpdesk.TicketSources.ITicketSourceRepository>().GetListAsync();
        var src = sources[0];

        var id = guidGen.Create();
        var ticket = new Ticket(
            id,
            $"TK-209905-{Guid.NewGuid():N}".Substring(0, 32),
            title,
            "desc",
            cat.Id, prio.Id, status.Id, src.Id,
            "Requester", "req@test.com",
            tags: tags);
        await ticketRepo.InsertAsync(ticket, autoSave: true);
        return ticket;
    }

    private AutomationRule BuildRule(AutomationTriggerType triggerType, int order = 0, bool stopProcessing = false)
    {
        var guidGen = GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        return new AutomationRule(
            guidGen.Create(),
            $"AutoRule_{Guid.NewGuid():N}",
            triggerType,
            executionOrder: order,
            isActive: true,
            stopProcessing: stopProcessing);
    }

    private async Task InsertRuleAsync(
        AutomationTriggerType triggers,
        List<RuleCondition> conditions,
        List<RuleAction> actions,
        int order = 0,
        bool stopProcessing = false)
    {
        var rule = BuildRule(triggers, order, stopProcessing);
        rule.SetConditions(conditions);
        rule.SetActions(actions);
        await InsertRawRuleAsync(rule);
    }

    private async Task InsertRawRuleAsync(AutomationRule rule)
    {
        await GetRequiredService<IRepository<AutomationRule, Guid>>().InsertAsync(rule, autoSave: true);
        _cleanupRuleIds.Add(rule.Id);
    }

    private async Task RunTriggersAsync(Ticket ticket, AutomationTriggerType triggerType)
    {
        var engine = GetRequiredService<AutomationRuleEngine>();
        await WithUnitOfWorkAsync(async () => await engine.ExecuteTriggersAsync(ticket, triggerType));
    }

    private static RuleAction Tag(string name) => new() { ActionType = AutomationActionType.AddTags, TargetValue = name };

    public override void Dispose()
    {
        try
        {
            var ruleRepo = GetRequiredService<IRepository<AutomationRule, Guid>>();
            var rules = ruleRepo.GetListAsync().GetAwaiter().GetResult();
            foreach (var r in rules.Where(r => _cleanupRuleIds.Contains(r.Id)))
            {
                ruleRepo.DeleteAsync(r, autoSave: true).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // ignore cleanup failures
        }
        base.Dispose();
    }
}
