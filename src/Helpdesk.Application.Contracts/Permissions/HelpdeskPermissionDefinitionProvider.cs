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

        // SLA
        var slaPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Sla.Default, L("Permission:Sla"));
        slaPermission.AddChild(HelpdeskPermissions.Sla.Policies, L("Permission:Sla.Policies"));
        slaPermission.AddChild(HelpdeskPermissions.Sla.BusinessHours, L("Permission:Sla.BusinessHours"));
        slaPermission.AddChild(HelpdeskPermissions.Sla.Reports, L("Permission:Sla.Reports"));

        // Dashboard
        helpdeskGroup.AddPermission(HelpdeskPermissions.Dashboard.Default, L("Permission:Dashboard"));

        // Knowledge Base
        var kbPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.KnowledgeBase.Default, L("Permission:KnowledgeBase"));
        kbPermission.AddChild(HelpdeskPermissions.KnowledgeBase.Create, L("Permission:KnowledgeBase.Create"));
        kbPermission.AddChild(HelpdeskPermissions.KnowledgeBase.Edit, L("Permission:KnowledgeBase.Edit"));
        kbPermission.AddChild(HelpdeskPermissions.KnowledgeBase.Delete, L("Permission:KnowledgeBase.Delete"));
        kbPermission.AddChild(HelpdeskPermissions.KnowledgeBase.Manage, L("Permission:KnowledgeBase.Manage"));

        // Customer Portal
        var portalPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.CustomerPortal.Default, L("Permission:CustomerPortal"));
        portalPermission.AddChild(HelpdeskPermissions.CustomerPortal.CreateTicket, L("Permission:CustomerPortal.CreateTicket"));

        // Assignment Rules
        var rulesPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.AssignmentRules.Default, L("Permission:AssignmentRules"));
        rulesPermission.AddChild(HelpdeskPermissions.AssignmentRules.Create, L("Permission:AssignmentRules.Create"));
        rulesPermission.AddChild(HelpdeskPermissions.AssignmentRules.Edit, L("Permission:AssignmentRules.Edit"));
        rulesPermission.AddChild(HelpdeskPermissions.AssignmentRules.Delete, L("Permission:AssignmentRules.Delete"));
        rulesPermission.AddChild(HelpdeskPermissions.AssignmentRules.Manage, L("Permission:AssignmentRules.Manage"));

        // Discord Settings
        var discordPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.DiscordSettings.Default, L("Permission:DiscordSettings"));
        discordPermission.AddChild(HelpdeskPermissions.DiscordSettings.Manage, L("Permission:DiscordSettings.Manage"));

        // Automations
        var automationsPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Automations.Default, L("Permission:Automations"));
        automationsPermission.AddChild(HelpdeskPermissions.Automations.Create, L("Permission:Automations.Create"));
        automationsPermission.AddChild(HelpdeskPermissions.Automations.Edit, L("Permission:Automations.Edit"));
        automationsPermission.AddChild(HelpdeskPermissions.Automations.Delete, L("Permission:Automations.Delete"));
        automationsPermission.AddChild(HelpdeskPermissions.Automations.Manage, L("Permission:Automations.Manage"));

        // Macros
        var macrosPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Macros.Default, L("Permission:Macros"));
        macrosPermission.AddChild(HelpdeskPermissions.Macros.Create, L("Permission:Macros.Create"));
        macrosPermission.AddChild(HelpdeskPermissions.Macros.Edit, L("Permission:Macros.Edit"));
        macrosPermission.AddChild(HelpdeskPermissions.Macros.Delete, L("Permission:Macros.Delete"));
        macrosPermission.AddChild(HelpdeskPermissions.Macros.Apply, L("Permission:Macros.Apply"));

        var aiSettingsPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.AiSettings.Default, L("Permission:AiSettings"));
        aiSettingsPermission.AddChild(HelpdeskPermissions.AiSettings.Manage, L("Permission:AiSettings.Manage"));

        var aiAssistantPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.AiAssistant.Default, L("Permission:AiAssistant"));
        aiAssistantPermission.AddChild(HelpdeskPermissions.AiAssistant.Use, L("Permission:AiAssistant.Use"));

        // Assets
        var assetsPermission = helpdeskGroup.AddPermission(HelpdeskPermissions.Assets.Default, L("Permission:Assets"));
        assetsPermission.AddChild(HelpdeskPermissions.Assets.Create, L("Permission:Assets.Create"));
        assetsPermission.AddChild(HelpdeskPermissions.Assets.Edit, L("Permission:Assets.Edit"));
        assetsPermission.AddChild(HelpdeskPermissions.Assets.Delete, L("Permission:Assets.Delete"));
        assetsPermission.AddChild(HelpdeskPermissions.Assets.Assign, L("Permission:Assets.Assign"));
        assetsPermission.AddChild(HelpdeskPermissions.Assets.ChangeStatus, L("Permission:Assets.ChangeStatus"));
    }


    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<HelpdeskResource>(name);
    }
}
