using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helpdesk.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Helpdesk.Departments;

public class EfCoreDepartmentRepository
    : EfCoreRepository<HelpdeskDbContext, Department, Guid>, IDepartmentRepository
{
    public EfCoreDepartmentRepository(IDbContextProvider<HelpdeskDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<Department?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.FirstOrDefaultAsync(x => x.Code == code, GetCancellationToken(cancellationToken));
    }

    public async Task<List<Department>> GetListAsync(
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
            .OrderBy(x => x.Name)
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
