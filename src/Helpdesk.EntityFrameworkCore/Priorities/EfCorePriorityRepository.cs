using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helpdesk.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Helpdesk.Priorities;

public class EfCorePriorityRepository
    : EfCoreRepository<HelpdeskDbContext, Priority, Guid>, IPriorityRepository
{
    public EfCorePriorityRepository(IDbContextProvider<HelpdeskDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<Priority?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.FirstOrDefaultAsync(x => x.Code == code, GetCancellationToken(cancellationToken));
    }

    public async Task<List<Priority>> GetListAsync(
        string? filter = null,
        bool? isActive = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .WhereIf(!filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(filter!) || x.Code.Contains(filter!))
            .WhereIf(isActive.HasValue, x => x.IsActive == isActive!.Value)
            .OrderBy(x => x.Order)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(
        string? filter = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .WhereIf(!filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(filter!) || x.Code.Contains(filter!))
            .WhereIf(isActive.HasValue, x => x.IsActive == isActive!.Value)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }
}
