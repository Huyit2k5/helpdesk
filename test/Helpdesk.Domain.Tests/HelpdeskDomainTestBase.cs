using Volo.Abp.Modularity;

namespace Helpdesk;

/* Inherit from this class for your domain layer tests. */
public abstract class HelpdeskDomainTestBase<TStartupModule> : HelpdeskTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
