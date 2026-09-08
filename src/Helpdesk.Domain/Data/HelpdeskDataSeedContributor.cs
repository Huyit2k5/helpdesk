using System;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Priorities;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace Helpdesk.Data;

/// <summary>
/// Seeds default master data on first run (via DbMigrator).
/// </summary>
public class HelpdeskDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPriorityRepository _priorityRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ITicketStatusRepository _ticketStatusRepository;
    private readonly ITicketSourceRepository _ticketSourceRepository;
    private readonly IGuidGenerator _guidGenerator;

    public HelpdeskDataSeedContributor(
        ICategoryRepository categoryRepository,
        IPriorityRepository priorityRepository,
        IDepartmentRepository departmentRepository,
        ITicketStatusRepository ticketStatusRepository,
        ITicketSourceRepository ticketSourceRepository,
        IGuidGenerator guidGenerator)
    {
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _departmentRepository = departmentRepository;
        _ticketStatusRepository = ticketStatusRepository;
        _ticketSourceRepository = ticketSourceRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedPrioritiesAsync();
        await SeedTicketStatusesAsync();
        await SeedTicketSourcesAsync();
        await SeedDepartmentsAsync();
        await SeedCategoriesAsync();
    }

    private async Task SeedPrioritiesAsync()
    {
        if (await _priorityRepository.GetCountAsync() > 0) return;

        await _priorityRepository.InsertAsync(new Priority(_guidGenerator.Create(), "Critical", "CRITICAL", "#DC2626", slaResponseHours: 1, slaResolutionHours: 4, order: 1));
        await _priorityRepository.InsertAsync(new Priority(_guidGenerator.Create(), "High", "HIGH", "#F59E0B", slaResponseHours: 4, slaResolutionHours: 8, order: 2));
        await _priorityRepository.InsertAsync(new Priority(_guidGenerator.Create(), "Medium", "MEDIUM", "#3B82F6", slaResponseHours: 8, slaResolutionHours: 24, order: 3));
        await _priorityRepository.InsertAsync(new Priority(_guidGenerator.Create(), "Low", "LOW", "#6B7280", slaResponseHours: 24, slaResolutionHours: 72, order: 4));
    }

    private async Task SeedTicketStatusesAsync()
    {
        if (await _ticketStatusRepository.GetCountAsync() > 0) return;

        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "New", "NEW", StatusGroup.Open, "#3B82F6", isDefault: true, order: 1));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "Assigned", "ASSIGNED", StatusGroup.Open, "#8B5CF6", order: 2));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "In Progress", "IN_PROGRESS", StatusGroup.InProgress, "#F59E0B", order: 3));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "Pending", "PENDING", StatusGroup.InProgress, "#EF4444", order: 4));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "Resolved", "RESOLVED", StatusGroup.Closed, "#10B981", order: 5));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "Closed", "CLOSED", StatusGroup.Closed, "#6B7280", isFinal: true, order: 6));
        await _ticketStatusRepository.InsertAsync(new TicketStatus(_guidGenerator.Create(), "Reopened", "REOPENED", StatusGroup.Open, "#F97316", order: 7));
    }

    private async Task SeedTicketSourcesAsync()
    {
        if (await _ticketSourceRepository.GetCountAsync() > 0) return;

        await _ticketSourceRepository.InsertAsync(new TicketSource(_guidGenerator.Create(), "Email", "EMAIL"));
        await _ticketSourceRepository.InsertAsync(new TicketSource(_guidGenerator.Create(), "Phone", "PHONE"));
        await _ticketSourceRepository.InsertAsync(new TicketSource(_guidGenerator.Create(), "Web Portal", "WEB_PORTAL"));
        await _ticketSourceRepository.InsertAsync(new TicketSource(_guidGenerator.Create(), "Chat", "CHAT"));
        await _ticketSourceRepository.InsertAsync(new TicketSource(_guidGenerator.Create(), "Walk-in", "WALK_IN"));
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _departmentRepository.GetCountAsync() > 0) return;

        await _departmentRepository.InsertAsync(new Department(_guidGenerator.Create(), "IT Support", "IT_SUPPORT", description: "Information Technology Support"));
        await _departmentRepository.InsertAsync(new Department(_guidGenerator.Create(), "Customer Service", "CUSTOMER_SERVICE", description: "Customer Service Department"));
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _categoryRepository.GetCountAsync() > 0) return;

        await _categoryRepository.InsertAsync(new Category(_guidGenerator.Create(), "Hardware", "HARDWARE", description: "Hardware related issues", order: 1));
        await _categoryRepository.InsertAsync(new Category(_guidGenerator.Create(), "Software", "SOFTWARE", description: "Software related issues", order: 2));
        await _categoryRepository.InsertAsync(new Category(_guidGenerator.Create(), "Network", "NETWORK", description: "Network and connectivity issues", order: 3));
        await _categoryRepository.InsertAsync(new Category(_guidGenerator.Create(), "Account", "ACCOUNT", description: "Account and access issues", order: 4));
        await _categoryRepository.InsertAsync(new Category(_guidGenerator.Create(), "General", "GENERAL", description: "General inquiries", order: 5));
    }
}
