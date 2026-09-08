using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Departments;

public interface IDepartmentRepository : IRepository<Department, Guid>
{
    Task<Department?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<Department>> GetListAsync(
        string? filter = null,
        bool? isActive = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? filter = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
}
