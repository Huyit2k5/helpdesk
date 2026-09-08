using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.CannedResponses;

public interface ICannedResponseRepository : IRepository<CannedResponse, Guid>
{
    Task<List<CannedResponse>> GetListAsync(
        string? filter = null,
        Guid? categoryId = null,
        bool? isPublic = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? filter = null,
        Guid? categoryId = null,
        bool? isPublic = null,
        CancellationToken cancellationToken = default);
}
