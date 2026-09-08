using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Categories;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    Task<Category?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<Category>> GetListAsync(
        string? filter = null,
        Guid? parentId = null,
        bool? isActive = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? filter = null,
        Guid? parentId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
}
