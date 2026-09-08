using System;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Helpdesk.TicketStatuses;

public class TicketStatusManager : DomainService
{
    private readonly ITicketStatusRepository _ticketStatusRepository;

    public TicketStatusManager(ITicketStatusRepository ticketStatusRepository)
    {
        _ticketStatusRepository = ticketStatusRepository;
    }

    public async Task<TicketStatus> CreateAsync(
        string name,
        string code,
        StatusGroup statusGroup,
        string? color = null,
        bool isFinal = false,
        bool isDefault = false,
        int order = 0)
    {
        await ValidateCodeAsync(code);

        return new TicketStatus(
            GuidGenerator.Create(),
            name,
            code,
            statusGroup,
            color,
            isFinal,
            isDefault,
            order);
    }

    public async Task ChangeCodeAsync(TicketStatus ticketStatus, string newCode)
    {
        Check.NotNull(ticketStatus, nameof(ticketStatus));

        if (ticketStatus.Code == newCode)
        {
            return;
        }

        await ValidateCodeAsync(newCode);
        ticketStatus.SetCode(newCode);
    }

    private async Task ValidateCodeAsync(string code)
    {
        var existing = await _ticketStatusRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            throw new BusinessException(HelpdeskDomainErrorCodes.TicketStatusCodeAlreadyExists)
                .WithData("code", code);
        }
    }
}
