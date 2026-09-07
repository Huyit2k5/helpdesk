using Helpdesk.Localization;
using Volo.Abp.Application.Services;

namespace Helpdesk;

/* Inherit your application services from this class.
 */
public abstract class HelpdeskAppService : ApplicationService
{
    protected HelpdeskAppService()
    {
        LocalizationResource = typeof(HelpdeskResource);
    }
}
