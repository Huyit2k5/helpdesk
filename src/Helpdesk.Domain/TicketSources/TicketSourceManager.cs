using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Helpdesk.TicketSources;

public class TicketSourceManager : DomainService
{
    private readonly ITicketSourceRepository _ticketSourceRepository;

    public TicketSourceManager(ITicketSourceRepository ticketSourceRepository)
    {
        _ticketSourceRepository = ticketSourceRepository;
    }

    public async Task<TicketSource> CreateAsync(
        string name,
        string code,
        bool isActive = true)
    {
        await ValidateCodeAsync(code);

        return new TicketSource(
            GuidGenerator.Create(),
            name,
            code,
            isActive);
    }

    public async Task ChangeCodeAsync(TicketSource ticketSource, string newCode)
    {
        Check.NotNull(ticketSource, nameof(ticketSource));

        if (ticketSource.Code == newCode)
        {
            return;
        }

        await ValidateCodeAsync(newCode);
        ticketSource.SetCode(newCode);
    }

    private async Task ValidateCodeAsync(string code)
    {
        var existing = await _ticketSourceRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            throw new BusinessException(HelpdeskDomainErrorCodes.TicketSourceCodeAlreadyExists)
                .WithData("code", code);
        }
    }
}
