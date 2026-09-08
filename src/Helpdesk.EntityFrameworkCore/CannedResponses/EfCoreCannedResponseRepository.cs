using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helpdesk.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Helpdesk.CannedResponses;

public class EfCoreCannedResponseRepository
    : EfCoreRepository<HelpdeskDbContext, CannedResponse, Guid>, ICannedResponseRepository
{
    public EfCoreCannedResponseRepository(IDbContextProvider<HelpdeskDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<List<CannedResponse>> GetListAsync(
        string? filter = null,
        Guid? categoryId = null,
        bool? isPublic = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .WhereIf(!filter.IsNullOrWhiteSpace(),
                x => x.Title.Contains(filter!) || x.Content.Contains(filter!))
            .WhereIf(categoryId.HasValue, x => x.CategoryId == categoryId)
            .WhereIf(isPublic.HasValue, x => x.IsPublic == isPublic!.Value)
            .OrderByDescending(x => x.UsageCount)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(
        string? filter = null,
        Guid? categoryId = null,
        bool? isPublic = null,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .WhereIf(!filter.IsNullOrWhiteSpace(),
                x => x.Title.Contains(filter!) || x.Content.Contains(filter!))
            .WhereIf(categoryId.HasValue, x => x.CategoryId == categoryId)
            .WhereIf(isPublic.HasValue, x => x.IsPublic == isPublic!.Value)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }
}
