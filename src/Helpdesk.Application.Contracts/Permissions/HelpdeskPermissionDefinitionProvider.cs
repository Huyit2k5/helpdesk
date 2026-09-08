using Helpdesk.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Permissions;

public class HelpdeskPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var helpdeskGroup = context.AddGroup(HelpdeskPermissions.GroupName, L("Permission:Helpdesk"));

        // Categories
        var categoriesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Categories.Default, L("Permission:Categories"));
        categoriesPermission.AddChild(HelpdeskPermissions.Categories.Create, L("Permission:Categories.Create"));
        categoriesPermission.AddChild(HelpdeskPermissions.Categories.Edit, L("Permission:Categories.Edit"));
        categoriesPermission.AddChild(HelpdeskPermissions.Categories.Delete, L("Permission:Categories.Delete"));

        // Priorities
        var prioritiesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Priorities.Default, L("Permission:Priorities"));
        prioritiesPermission.AddChild(HelpdeskPermissions.Priorities.Create, L("Permission:Priorities.Create"));
        prioritiesPermission.AddChild(HelpdeskPermissions.Priorities.Edit, L("Permission:Priorities.Edit"));
        prioritiesPermission.AddChild(HelpdeskPermissions.Priorities.Delete, L("Permission:Priorities.Delete"));

        // Departments
        var departmentsPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Departments.Default, L("Permission:Departments"));
        departmentsPermission.AddChild(HelpdeskPermissions.Departments.Create, L("Permission:Departments.Create"));
        departmentsPermission.AddChild(HelpdeskPermissions.Departments.Edit, L("Permission:Departments.Edit"));
        departmentsPermission.AddChild(HelpdeskPermissions.Departments.Delete, L("Permission:Departments.Delete"));

        // Ticket Statuses
        var ticketStatusesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.TicketStatuses.Default, L("Permission:TicketStatuses"));
        ticketStatusesPermission.AddChild(HelpdeskPermissions.TicketStatuses.Create, L("Permission:TicketStatuses.Create"));
        ticketStatusesPermission.AddChild(HelpdeskPermissions.TicketStatuses.Edit, L("Permission:TicketStatuses.Edit"));
        ticketStatusesPermission.AddChild(HelpdeskPermissions.TicketStatuses.Delete, L("Permission:TicketStatuses.Delete"));

        // Ticket Sources
        var ticketSourcesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.TicketSources.Default, L("Permission:TicketSources"));
        ticketSourcesPermission.AddChild(HelpdeskPermissions.TicketSources.Create, L("Permission:TicketSources.Create"));
        ticketSourcesPermission.AddChild(HelpdeskPermissions.TicketSources.Edit, L("Permission:TicketSources.Edit"));
        ticketSourcesPermission.AddChild(HelpdeskPermissions.TicketSources.Delete, L("Permission:TicketSources.Delete"));

        // Canned Responses
        var cannedResponsesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.CannedResponses.Default, L("Permission:CannedResponses"));
        cannedResponsesPermission.AddChild(HelpdeskPermissions.CannedResponses.Create, L("Permission:CannedResponses.Create"));
        cannedResponsesPermission.AddChild(HelpdeskPermissions.CannedResponses.Edit, L("Permission:CannedResponses.Edit"));
        cannedResponsesPermission.AddChild(HelpdeskPermissions.CannedResponses.Delete, L("Permission:CannedResponses.Delete"));

        // Tickets
        var ticketsPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Tickets.Default, L("Permission:Tickets"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.Create, L("Permission:Tickets.Create"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.Edit, L("Permission:Tickets.Edit"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.Delete, L("Permission:Tickets.Delete"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.Assign, L("Permission:Tickets.Assign"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.ChangeStatus, L("Permission:Tickets.ChangeStatus"));
        ticketsPermission.AddChild(HelpdeskPermissions.Tickets.AddComment, L("Permission:Tickets.AddComment"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HelpdeskResource>(name);
    }
}
