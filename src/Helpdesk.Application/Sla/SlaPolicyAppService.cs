using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.Permissions;
using Helpdesk.Priorities;
using Helpdesk.Sla.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Sla;

[Authorize(HelpdeskPermissions.Sla.Policies)]
public class SlaPolicyAppService : ApplicationService, ISlaPolicyAppService
{
    private readonly IRepository<SlaPolicy, Guid> _policyRepository;
    private readonly IRepository<SlaPolicyRule, Guid> _ruleRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;

    public SlaPolicyAppService(
        IRepository<SlaPolicy, Guid> policyRepository,
        IRepository<SlaPolicyRule, Guid> ruleRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<Category, Guid> categoryRepository)
    {
        _policyRepository = policyRepository;
        _ruleRepository = ruleRepository;
        _priorityRepository = priorityRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResultDto<SlaPolicyDto>> GetListAsync(GetSlaPolicyListInput input)
    {
        var query = await _policyRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            query = query.Where(p => p.Name.Contains(input.Filter));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var sorting = string.IsNullOrWhiteSpace(input.Sorting) ? nameof(SlaPolicy.CreationTime) + " desc" : input.Sorting;
        var maxCount = input.MaxResultCount <= 0 ? 10 : input.MaxResultCount;
        var skipCount = input.SkipCount < 0 ? 0 : input.SkipCount;

        var policies = await AsyncExecuter.ToListAsync(
            query.OrderBy(sorting).Skip(skipCount).Take(maxCount));

        var policyIds = policies.Select(p => p.Id).ToList();
        var allRules = await _ruleRepository.GetListAsync(r => policyIds.Contains(r.SlaPolicyId));
        var rulesByPolicy = allRules.GroupBy(r => r.SlaPolicyId).ToDictionary(g => g.Key, g => g.ToList());

        var priorities = await _priorityRepository.GetListAsync();
        var categories = await _categoryRepository.GetListAsync();

        var dtos = policies.Select(p => MapToDto(
            p,
            rulesByPolicy.GetValueOrDefault(p.Id) ?? new List<SlaPolicyRule>(),
            priorities,
            categories)).ToList();

        return new PagedResultDto<SlaPolicyDto>(totalCount, dtos);
    }

    public async Task<SlaPolicyDto> GetAsync(Guid id)
    {
        var policy = await _policyRepository.GetAsync(id);
        var rules = await _ruleRepository.GetListAsync(r => r.SlaPolicyId == id);
        var priorities = await _priorityRepository.GetListAsync();
        var categories = await _categoryRepository.GetListAsync();

        return MapToDto(policy, rules, priorities, categories);
    }

    public async Task<SlaPolicyDto> CreateAsync(CreateUpdateSlaPolicyDto input)
    {
        if (input.IsDefault)
        {
            await UnsetOtherDefaultsAsync();
        }

        var policy = new SlaPolicy(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.IsDefault,
            input.IsActive,
            CurrentTenant.Id);

        if (input.Rules != null)
        {
            foreach (var rule in input.Rules)
            {
                policy.AddRule(
                    GuidGenerator.Create(),
                    rule.PriorityId,
                    rule.CategoryId,
                    rule.ResponseTimeMinutes,
                    rule.ResolutionTimeMinutes);
            }
        }

        await _policyRepository.InsertAsync(policy, autoSave: true);
        return await GetAsync(policy.Id);
    }

    public async Task<SlaPolicyDto> UpdateAsync(Guid id, CreateUpdateSlaPolicyDto input)
    {
        var policy = await _policyRepository.GetAsync(id);

        if (input.IsDefault && !policy.IsDefault)
        {
            await UnsetOtherDefaultsAsync(id);
        }

        policy.SetName(input.Name);
        policy.Description = input.Description;
        policy.IsDefault = input.IsDefault;
        policy.IsActive = input.IsActive;

        policy.ClearRules();
        if (input.Rules != null)
        {
            foreach (var rule in input.Rules)
            {
                policy.AddRule(
                    GuidGenerator.Create(),
                    rule.PriorityId,
                    rule.CategoryId,
                    rule.ResponseTimeMinutes,
                    rule.ResolutionTimeMinutes);
            }
        }

        await _policyRepository.UpdateAsync(policy, autoSave: true);
        return await GetAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var policy = await _policyRepository.GetAsync(id);
        if (policy.IsDefault)
        {
            throw new UserFriendlyException("Cannot delete default SLA policy.");
        }

        await _policyRepository.DeleteAsync(policy, autoSave: true);
    }

    public async Task<List<SlaPolicyDto>> GetActivePolicyListAsync()
    {
        var policies = await _policyRepository.GetListAsync(p => p.IsActive);
        return policies.Select(p => new SlaPolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IsDefault = p.IsDefault,
            IsActive = p.IsActive
        }).ToList();
    }

    private async Task UnsetOtherDefaultsAsync(Guid? excludeId = null)
    {
        var query = await _policyRepository.GetQueryableAsync();
        var defaultPolicies = await AsyncExecuter.ToListAsync(
            query.Where(p => p.IsDefault && (!excludeId.HasValue || p.Id != excludeId.Value)));

        foreach (var p in defaultPolicies)
        {
            p.IsDefault = false;
            await _policyRepository.UpdateAsync(p);
        }
    }

    private static SlaPolicyDto MapToDto(SlaPolicy policy, List<SlaPolicyRule> rules, List<Priority> priorities, List<Category> categories)
    {
        var priorityDict = priorities.ToDictionary(x => x.Id, x => x.Name);
        var categoryDict = categories.ToDictionary(x => x.Id, x => x.Name);

        return new SlaPolicyDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Description = policy.Description,
            IsDefault = policy.IsDefault,
            IsActive = policy.IsActive,
            CreationTime = policy.CreationTime,
            CreatorId = policy.CreatorId,
            Rules = (rules ?? new List<SlaPolicyRule>()).Select(r => new SlaPolicyRuleDto
            {
                Id = r.Id,
                SlaPolicyId = r.SlaPolicyId,
                PriorityId = r.PriorityId,
                PriorityName = priorityDict.TryGetValue(r.PriorityId, out var pName) ? pName : string.Empty,
                CategoryId = r.CategoryId,
                CategoryName = r.CategoryId.HasValue && categoryDict.TryGetValue(r.CategoryId.Value, out var cName) ? cName : null,
                ResponseTimeMinutes = r.ResponseTimeMinutes,
                ResolutionTimeMinutes = r.ResolutionTimeMinutes
            }).ToList()
        };
    }
}
