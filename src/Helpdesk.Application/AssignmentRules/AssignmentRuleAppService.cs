using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.AssignmentRules.Dtos;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Permissions;
using Helpdesk.Priorities;
using Helpdesk.TicketSources;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Helpdesk.AssignmentRules;

[Authorize(HelpdeskPermissions.AssignmentRules.Default)]
public class AssignmentRuleAppService : ApplicationService, IAssignmentRuleAppService
{
    private readonly IRepository<AssignmentRule, Guid> _ruleRepository;
    private readonly IRepository<AssignmentRuleAgent, Guid> _agentRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<TicketSource, Guid> _sourceRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public AssignmentRuleAppService(
        IRepository<AssignmentRule, Guid> ruleRepository,
        IRepository<AssignmentRuleAgent, Guid> agentRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<TicketSource, Guid> sourceRepository,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _ruleRepository = ruleRepository;
        _agentRepository = agentRepository;
        _departmentRepository = departmentRepository;
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _sourceRepository = sourceRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResultDto<AssignmentRuleDto>> GetListAsync(AssignmentRuleGetListInput input)
    {
        var ruleQuery = await _ruleRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            ruleQuery = ruleQuery.Where(r => r.Name.ToLower().Contains(filter) || (r.Description != null && r.Description.ToLower().Contains(filter)));
        }

        var totalCount = await AsyncExecuter.CountAsync(ruleQuery);

        ruleQuery = ruleQuery
            .OrderBy(r => r.Order)
            .ThenByDescending(r => r.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var rules = await AsyncExecuter.ToListAsync(ruleQuery);
        var ruleIds = rules.Select(r => r.Id).ToList();

        var agentQuery = await _agentRepository.GetQueryableAsync();
        var allAgents = await AsyncExecuter.ToListAsync(
            agentQuery.Where(a => ruleIds.Contains(a.RuleId)).OrderBy(a => a.Order)
        );

        var userQuery = await _userRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(userQuery);
        var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

        var deptQuery = await _departmentRepository.GetQueryableAsync();
        var depts = await AsyncExecuter.ToListAsync(deptQuery);
        var deptDict = depts.ToDictionary(d => d.Id, d => d.Name);

        var catQuery = await _categoryRepository.GetQueryableAsync();
        var cats = await AsyncExecuter.ToListAsync(catQuery);
        var catDict = cats.ToDictionary(c => c.Id, c => c.Name);

        var priQuery = await _priorityRepository.GetQueryableAsync();
        var pris = await AsyncExecuter.ToListAsync(priQuery);
        var priDict = pris.ToDictionary(p => p.Id, p => p.Name);

        var srcQuery = await _sourceRepository.GetQueryableAsync();
        var srcs = await AsyncExecuter.ToListAsync(srcQuery);
        var srcDict = srcs.ToDictionary(s => s.Id, s => s.Name);

        var dtos = rules.Select(r =>
        {
            var ruleAgents = allAgents.Where(a => a.RuleId == r.Id).Select(a => new AssignmentRuleAgentDto
            {
                Id = a.Id,
                RuleId = a.RuleId,
                UserId = a.UserId,
                UserName = userDict.GetValueOrDefault(a.UserId, "Nhân viên"),
                FullName = userDict.GetValueOrDefault(a.UserId, "Nhân viên"),
                Order = a.Order
            }).ToList();

            return new AssignmentRuleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Order = r.Order,
                IsActive = r.IsActive,
                RoutingStrategy = r.RoutingStrategy,
                DepartmentId = r.DepartmentId,
                DepartmentName = r.DepartmentId.HasValue ? deptDict.GetValueOrDefault(r.DepartmentId.Value) : null,
                CategoryId = r.CategoryId,
                CategoryName = r.CategoryId.HasValue ? catDict.GetValueOrDefault(r.CategoryId.Value) : null,
                PriorityId = r.PriorityId,
                PriorityName = r.PriorityId.HasValue ? priDict.GetValueOrDefault(r.PriorityId.Value) : null,
                SourceId = r.SourceId,
                SourceName = r.SourceId.HasValue ? srcDict.GetValueOrDefault(r.SourceId.Value) : null,
                DirectAssigneeId = r.DirectAssigneeId,
                DirectAssigneeName = r.DirectAssigneeId.HasValue ? userDict.GetValueOrDefault(r.DirectAssigneeId.Value) : null,
                LastAssignedUserId = r.LastAssignedUserId,
                Agents = ruleAgents,
                CreationTime = r.CreationTime
            };
        }).ToList();

        return new PagedResultDto<AssignmentRuleDto>(totalCount, dtos);
    }

    public async Task<AssignmentRuleDto> GetAsync(Guid id)
    {
        var rule = await _ruleRepository.GetAsync(id);
        var agentQuery = await _agentRepository.GetQueryableAsync();
        var agents = await AsyncExecuter.ToListAsync(
            agentQuery.Where(a => a.RuleId == id).OrderBy(a => a.Order)
        );

        var userQuery = await _userRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(userQuery);
        var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

        var dept = rule.DepartmentId.HasValue ? await _departmentRepository.FindAsync(rule.DepartmentId.Value) : null;
        var cat = rule.CategoryId.HasValue ? await _categoryRepository.FindAsync(rule.CategoryId.Value) : null;
        var pri = rule.PriorityId.HasValue ? await _priorityRepository.FindAsync(rule.PriorityId.Value) : null;
        var src = rule.SourceId.HasValue ? await _sourceRepository.FindAsync(rule.SourceId.Value) : null;

        return new AssignmentRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            Order = rule.Order,
            IsActive = rule.IsActive,
            RoutingStrategy = rule.RoutingStrategy,
            DepartmentId = rule.DepartmentId,
            DepartmentName = dept?.Name,
            CategoryId = rule.CategoryId,
            CategoryName = cat?.Name,
            PriorityId = rule.PriorityId,
            PriorityName = pri?.Name,
            SourceId = rule.SourceId,
            SourceName = src?.Name,
            DirectAssigneeId = rule.DirectAssigneeId,
            DirectAssigneeName = rule.DirectAssigneeId.HasValue ? userDict.GetValueOrDefault(rule.DirectAssigneeId.Value) : null,
            LastAssignedUserId = rule.LastAssignedUserId,
            Agents = agents.Select(a => new AssignmentRuleAgentDto
            {
                Id = a.Id,
                RuleId = a.RuleId,
                UserId = a.UserId,
                UserName = userDict.GetValueOrDefault(a.UserId, "Nhân viên"),
                Order = a.Order
            }).ToList(),
            CreationTime = rule.CreationTime
        };
    }

    [Authorize(HelpdeskPermissions.AssignmentRules.Create)]
    public async Task<AssignmentRuleDto> CreateAsync(CreateUpdateAssignmentRuleDto input)
    {
        var rule = new AssignmentRule(
            GuidGenerator.Create(),
            input.Name,
            input.Order,
            input.RoutingStrategy,
            input.Description,
            input.DepartmentId,
            input.CategoryId,
            input.PriorityId,
            input.SourceId,
            input.DirectAssigneeId,
            input.IsActive
        );

        await _ruleRepository.InsertAsync(rule, autoSave: true);

        if (input.AgentUserIds != null && input.AgentUserIds.Any())
        {
            int order = 0;
            foreach (var userId in input.AgentUserIds.Distinct())
            {
                var agent = new AssignmentRuleAgent(GuidGenerator.Create(), rule.Id, userId, order++);
                await _agentRepository.InsertAsync(agent);
            }
        }

        return await GetAsync(rule.Id);
    }

    [Authorize(HelpdeskPermissions.AssignmentRules.Edit)]
    public async Task<AssignmentRuleDto> UpdateAsync(Guid id, CreateUpdateAssignmentRuleDto input)
    {
        var rule = await _ruleRepository.GetAsync(id);

        rule.SetName(input.Name);
        rule.Description = input.Description;
        rule.Order = input.Order;
        rule.IsActive = input.IsActive;
        rule.RoutingStrategy = input.RoutingStrategy;
        rule.DepartmentId = input.DepartmentId;
        rule.CategoryId = input.CategoryId;
        rule.PriorityId = input.PriorityId;
        rule.SourceId = input.SourceId;
        rule.DirectAssigneeId = input.DirectAssigneeId;

        await _ruleRepository.UpdateAsync(rule, autoSave: true);

        // Cập nhật danh sách agents
        var agentQuery = await _agentRepository.GetQueryableAsync();
        var oldAgents = await AsyncExecuter.ToListAsync(agentQuery.Where(a => a.RuleId == id));
        foreach (var old in oldAgents)
        {
            await _agentRepository.DeleteAsync(old);
        }

        if (input.AgentUserIds != null && input.AgentUserIds.Any())
        {
            int order = 0;
            foreach (var userId in input.AgentUserIds.Distinct())
            {
                var agent = new AssignmentRuleAgent(GuidGenerator.Create(), rule.Id, userId, order++);
                await _agentRepository.InsertAsync(agent);
            }
        }

        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.AssignmentRules.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var agentQuery = await _agentRepository.GetQueryableAsync();
        var agents = await AsyncExecuter.ToListAsync(agentQuery.Where(a => a.RuleId == id));
        foreach (var agent in agents)
        {
            await _agentRepository.DeleteAsync(agent);
        }

        await _ruleRepository.DeleteAsync(id);
    }

    [Authorize(HelpdeskPermissions.AssignmentRules.Edit)]
    public async Task<AssignmentRuleDto> ToggleActiveAsync(Guid id)
    {
        var rule = await _ruleRepository.GetAsync(id);
        rule.IsActive = !rule.IsActive;
        await _ruleRepository.UpdateAsync(rule, autoSave: true);
        return await GetAsync(id);
    }

    public async Task<List<AgentLookupDto>> GetAgentLookupAsync()
    {
        var userQuery = await _userRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(userQuery.Where(u => u.IsActive));

        return users.Select(u => new AgentLookupDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Name = u.Name,
            Email = u.Email
        }).OrderBy(u => u.UserName).ToList();
    }
}
