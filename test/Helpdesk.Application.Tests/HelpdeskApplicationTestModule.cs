using Volo.Abp.Modularity;

namespace Helpdesk;

[DependsOn(
    typeof(HelpdeskApplicationModule),
    typeof(HelpdeskDomainTestModule)
)]
public class HelpdeskApplicationTestModule : AbpModule
{

}
