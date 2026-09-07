using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Helpdesk.Data;
using Volo.Abp.DependencyInjection;

namespace Helpdesk.EntityFrameworkCore;

public class EntityFrameworkCoreHelpdeskDbSchemaMigrator
    : IHelpdeskDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreHelpdeskDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the HelpdeskDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<HelpdeskDbContext>()
            .Database
            .MigrateAsync();
    }
}
