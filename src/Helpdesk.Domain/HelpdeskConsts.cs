using Volo.Abp.Identity;

namespace Helpdesk;

public static class HelpdeskConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;
    public const string AdminEmailDefaultValue = IdentityDataSeedContributor.AdminEmailDefaultValue;
    public const string AdminPasswordDefaultValue = "Huy123@";
}
