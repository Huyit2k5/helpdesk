using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Helpdesk.Data;

/* This is used if database provider does't define
 * IHelpdeskDbSchemaMigrator implementation.
 */
public class NullHelpdeskDbSchemaMigrator : IHelpdeskDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
