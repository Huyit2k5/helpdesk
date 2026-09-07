using Volo.Abp.Modularity;

namespace Helpdesk;

public abstract class HelpdeskApplicationTestBase<TStartupModule> : HelpdeskTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
