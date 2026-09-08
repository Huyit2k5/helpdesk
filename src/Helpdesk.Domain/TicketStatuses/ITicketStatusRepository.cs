using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.TicketStatuses;

public interface ITicketStatusRepository : IRepository<TicketStatus, Guid>
{
    Task<TicketStatus?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<TicketStatus?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<List<TicketStatus>> GetListAsync(
        string? filter = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? filter = null,
        CancellationToken cancellationToken = default);
}
