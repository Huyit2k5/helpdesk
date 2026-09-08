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
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.BusinessHour, Guid> _businessHourRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.Holiday, Guid> _holidayRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.SlaPolicy, Guid> _slaPolicyRepository;

    public HelpdeskDataSeedContributor(
        ICategoryRepository categoryRepository,
        IPriorityRepository priorityRepository,
        IDepartmentRepository departmentRepository,
        ITicketStatusRepository ticketStatusRepository,
        ITicketSourceRepository ticketSourceRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.BusinessHour, Guid> businessHourRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.Holiday, Guid> holidayRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.SlaPolicy, Guid> slaPolicyRepository,
        IGuidGenerator guidGenerator)
    {
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _departmentRepository = departmentRepository;
        _ticketStatusRepository = ticketStatusRepository;
        _ticketSourceRepository = ticketSourceRepository;
        _businessHourRepository = businessHourRepository;
        _holidayRepository = holidayRepository;
        _slaPolicyRepository = slaPolicyRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedPrioritiesAsync();
        await SeedTicketStatusesAsync();
        await SeedTicketSourcesAsync();
        await SeedDepartmentsAsync();
        await SeedCategoriesAsync();
        await SeedBusinessHoursAsync();
        await SeedHolidaysAsync();
        await SeedSlaPoliciesAsync();
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

    private async Task SeedBusinessHoursAsync()
    {
        if (await _businessHourRepository.GetCountAsync() > 0) return;

        var start = new TimeSpan(8, 30, 0);
        var end = new TimeSpan(17, 30, 0);

        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Monday, start, end, isWorkDay: true));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Tuesday, start, end, isWorkDay: true));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Wednesday, start, end, isWorkDay: true));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Thursday, start, end, isWorkDay: true));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Friday, start, end, isWorkDay: true));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Saturday, start, end, isWorkDay: false));
        await _businessHourRepository.InsertAsync(new Helpdesk.Sla.BusinessHour(_guidGenerator.Create(), DayOfWeek.Sunday, start, end, isWorkDay: false));
    }

    private async Task SeedHolidaysAsync()
    {
        if (await _holidayRepository.GetCountAsync() > 0) return;

        await _holidayRepository.InsertAsync(new Helpdesk.Sla.Holiday(_guidGenerator.Create(), "Tết Dương Lịch", new DateTime(DateTime.UtcNow.Year, 1, 1), isRecurring: true));
        await _holidayRepository.InsertAsync(new Helpdesk.Sla.Holiday(_guidGenerator.Create(), "Ngày Quốc Khánh", new DateTime(DateTime.UtcNow.Year, 9, 2), isRecurring: true));
        await _holidayRepository.InsertAsync(new Helpdesk.Sla.Holiday(_guidGenerator.Create(), "Giải Phóng Miền Nam", new DateTime(DateTime.UtcNow.Year, 4, 30), isRecurring: true));
        await _holidayRepository.InsertAsync(new Helpdesk.Sla.Holiday(_guidGenerator.Create(), "Quốc Tế Lao Động", new DateTime(DateTime.UtcNow.Year, 5, 1), isRecurring: true));
    }

    private async Task SeedSlaPoliciesAsync()
    {
        if (await _slaPolicyRepository.GetCountAsync() > 0) return;

        var defaultPolicy = new Helpdesk.Sla.SlaPolicy(
            _guidGenerator.Create(),
            "Chính Sách SLA Tiêu Chuẩn",
            "Chính sách SLA mặc định áp dụng theo mức độ ưu tiên của sự vụ",
            isDefault: true,
            isActive: true);

        var priorities = await _priorityRepository.GetListAsync();
        foreach (var p in priorities)
        {
            int responseMins = p.SlaResponseHours > 0 ? p.SlaResponseHours * 60 : 4 * 60;
            int resolutionMins = p.SlaResolutionHours > 0 ? p.SlaResolutionHours * 60 : 24 * 60;

            defaultPolicy.AddRule(
                _guidGenerator.Create(),
                p.Id,
                categoryId: null,
                responseTimeMinutes: responseMins,
                resolutionTimeMinutes: resolutionMins,
                escalationEnabled: true);
        }

        await _slaPolicyRepository.InsertAsync(defaultPolicy, autoSave: true);
    }
}
