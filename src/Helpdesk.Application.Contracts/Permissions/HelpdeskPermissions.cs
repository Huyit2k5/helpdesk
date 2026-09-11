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

    public static class KnowledgeBase
    {
        public const string Default = GroupName + ".KnowledgeBase";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Manage = Default + ".Manage";
    }

    public static class CustomerPortal
    {
        public const string Default = GroupName + ".CustomerPortal";
        public const string CreateTicket = Default + ".CreateTicket";
    }

    public static class AssignmentRules
    {
        public const string Default = GroupName + ".AssignmentRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Manage = Default + ".Manage";
    }

    public static class DiscordSettings
    {
        public const string Default = GroupName + ".DiscordSettings";
        public const string Manage = Default + ".Manage";
    }

    public static class Automations
    {
        public const string Default = GroupName + ".Automations";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Manage = Default + ".Manage";
    }

    public static class Macros
    {
        public const string Default = GroupName + ".Macros";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Apply = Default + ".Apply";
    }

    public static class AiSettings
    {
        public const string Default = GroupName + ".AiSettings";
        public const string Manage = Default + ".Manage";
    }

    public static class AiAssistant
    {
        public const string Default = GroupName + ".AiAssistant";
        public const string Use = Default + ".Use";
    }
}

