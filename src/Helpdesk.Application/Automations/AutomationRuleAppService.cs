using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Automations.Dtos;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Automations;

[Authorize(HelpdeskPermissions.Automations.Default)]
public class AutomationRuleAppService : ApplicationService, IAutomationRuleAppService
{
    private readonly IRepository<AutomationRule, Guid> _ruleRepository;

    public AutomationRuleAppService(IRepository<AutomationRule, Guid> ruleRepository)
    {
        _ruleRepository = ruleRepository;
    }

    public async Task<PagedResultDto<AutomationRuleDto>> GetListAsync(GetAutomationRuleListInput input)
    {
        var query = await _ruleRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(filter) || (r.Description != null && r.Description.ToLower().Contains(filter)));
        }

        if (input.TriggerType.HasValue)
        {
            query = query.Where(r => r.TriggerType == input.TriggerType.Value);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(r => r.ExecutionOrder).PageBy(input)
        );

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResultDto<AutomationRuleDto>(totalCount, dtos);
    }

    public async Task<AutomationRuleDto> GetAsync(Guid id)
    {
        var rule = await _ruleRepository.GetAsync(id);
        return MapToDto(rule);
    }

    [Authorize(HelpdeskPermissions.Automations.Create)]
    public async Task<AutomationRuleDto> CreateAsync(CreateUpdateAutomationRuleDto input)
    {
        var rule = new AutomationRule(
            GuidGenerator.Create(),
            input.Name,
            input.TriggerType,
            input.ExecutionOrder,
            input.Description,
            input.IsActive,
            input.StopProcessing
        );

        rule.SetConditions(input.Conditions);
        rule.SetActions(input.Actions);

        await _ruleRepository.InsertAsync(rule, autoSave: true);
        return MapToDto(rule);
    }

    [Authorize(HelpdeskPermissions.Automations.Edit)]
    public async Task<AutomationRuleDto> UpdateAsync(Guid id, CreateUpdateAutomationRuleDto input)
    {
        var rule = await _ruleRepository.GetAsync(id);

        rule.SetName(input.Name);
        rule.Description = input.Description;
        rule.TriggerType = input.TriggerType;
        rule.ExecutionOrder = input.ExecutionOrder;
        rule.IsActive = input.IsActive;
        rule.StopProcessing = input.StopProcessing;

        rule.SetConditions(input.Conditions);
        rule.SetActions(input.Actions);

        await _ruleRepository.UpdateAsync(rule, autoSave: true);
        return MapToDto(rule);
    }

    [Authorize(HelpdeskPermissions.Automations.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _ruleRepository.DeleteAsync(id);
    }

    [Authorize(HelpdeskPermissions.Automations.Edit)]
    public async Task<AutomationRuleDto> ToggleActiveAsync(Guid id)
    {
        var rule = await _ruleRepository.GetAsync(id);
        rule.IsActive = !rule.IsActive;
        await _ruleRepository.UpdateAsync(rule, autoSave: true);
        return MapToDto(rule);
    }

    private static AutomationRuleDto MapToDto(AutomationRule rule)
    {
        return new AutomationRuleDto
        {
            Id = rule.Id,
            CreationTime = rule.CreationTime,
            CreatorId = rule.CreatorId,
            LastModificationTime = rule.LastModificationTime,
            LastModifierId = rule.LastModifierId,
            Name = rule.Name,
            Description = rule.Description,
            TriggerType = rule.TriggerType,
            ExecutionOrder = rule.ExecutionOrder,
            IsActive = rule.IsActive,
            StopProcessing = rule.StopProcessing,
            Conditions = rule.GetConditions(),
            Actions = rule.GetActions()
        };
    }
}
