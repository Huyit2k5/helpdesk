using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Helpdesk.Priorities;

public class PriorityManager : DomainService
{
    private readonly IPriorityRepository _priorityRepository;

    public PriorityManager(IPriorityRepository priorityRepository)
    {
        _priorityRepository = priorityRepository;
    }

    public async Task<Priority> CreateAsync(
        string name,
        string code,
        string? color = null,
        int slaResponseHours = 0,
        int slaResolutionHours = 0,
        int order = 0,
        bool isActive = true)
    {
        await ValidateCodeAsync(code);

        return new Priority(
            GuidGenerator.Create(),
            name,
            code,
            color,
            slaResponseHours,
            slaResolutionHours,
            order,
            isActive);
    }

    public async Task ChangeCodeAsync(Priority priority, string newCode)
    {
        Check.NotNull(priority, nameof(priority));

        if (priority.Code == newCode)
        {
            return;
        }

        await ValidateCodeAsync(newCode);
        priority.SetCode(newCode);
    }

    private async Task ValidateCodeAsync(string code)
    {
        var existing = await _priorityRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            throw new BusinessException(HelpdeskDomainErrorCodes.PriorityCodeAlreadyExists)
                .WithData("code", code);
        }
    }
}
