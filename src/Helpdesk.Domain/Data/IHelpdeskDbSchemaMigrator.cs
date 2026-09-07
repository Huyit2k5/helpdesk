using System.Threading.Tasks;

namespace Helpdesk.Data;

public interface IHelpdeskDbSchemaMigrator
{
    Task MigrateAsync();
}
