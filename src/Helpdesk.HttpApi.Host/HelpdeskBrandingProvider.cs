using Microsoft.Extensions.Localization;
using Helpdesk.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Helpdesk;

[Dependency(ReplaceServices = true)]
public class HelpdeskBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<HelpdeskResource> _localizer;

    public HelpdeskBrandingProvider(IStringLocalizer<HelpdeskResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
