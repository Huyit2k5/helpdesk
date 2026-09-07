using Helpdesk.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Permissions;

public class HelpdeskPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(HelpdeskPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(HelpdeskPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HelpdeskResource>(name);
    }
}
