using Volo.Abp.Modularity;

namespace Helpdesk;

[DependsOn(
    typeof(HelpdeskDomainModule),
    typeof(HelpdeskTestBaseModule)
)]
public class HelpdeskDomainTestModule : AbpModule
{

}
