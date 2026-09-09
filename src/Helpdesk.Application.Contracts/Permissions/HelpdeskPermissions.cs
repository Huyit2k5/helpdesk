namespace Helpdesk.Permissions;

public static class HelpdeskPermissions
{
    public const string GroupName = "Helpdesk";

    public static class Categories
    {
        public const string Default = GroupName + ".Categories";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Priorities
    {
        public const string Default = GroupName + ".Priorities";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Departments
    {
        public const string Default = GroupName + ".Departments";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class TicketStatuses
    {
        public const string Default = GroupName + ".TicketStatuses";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class TicketSources
    {
        public const string Default = GroupName + ".TicketSources";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class CannedResponses
    {
        public const string Default = GroupName + ".CannedResponses";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Tickets
    {
        public const string Default = GroupName + ".Tickets";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Assign = Default + ".Assign";
        public const string ChangeStatus = Default + ".ChangeStatus";
        public const string AddComment = Default + ".AddComment";
    }

    public static class Sla
    {
        public const string Default = GroupName + ".Sla";
        public const string Policies = Default + ".Policies";
        public const string BusinessHours = Default + ".BusinessHours";
        public const string Reports = Default + ".Reports";
    }

    public static class Dashboard
    {
        public const string Default = GroupName + ".Dashboard";
    }
}
