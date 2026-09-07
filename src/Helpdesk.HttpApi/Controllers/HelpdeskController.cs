using Helpdesk.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Helpdesk.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class HelpdeskController : AbpControllerBase
{
    protected HelpdeskController()
    {
        LocalizationResource = typeof(HelpdeskResource);
    }
}
