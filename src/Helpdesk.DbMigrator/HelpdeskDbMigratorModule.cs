using Helpdesk.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Helpdesk.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(HelpdeskEntityFrameworkCoreModule),
    typeof(HelpdeskApplicationContractsModule)
)]
public class HelpdeskDbMigratorModule : AbpModule
{
}
