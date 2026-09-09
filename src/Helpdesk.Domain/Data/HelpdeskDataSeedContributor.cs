using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Tickets.Ticket, Guid> _ticketRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Tickets.TicketActivity, Guid> _ticketActivityRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.KnowledgeBase.KnowledgeArticle, Guid> _articleRepository;
    private readonly Volo.Abp.Identity.IdentityRoleManager _roleManager;
    private readonly Volo.Abp.Identity.IdentityUserManager _userManager;
    private readonly Volo.Abp.PermissionManagement.IPermissionDataSeeder _permissionDataSeeder;

    public HelpdeskDataSeedContributor(
        ICategoryRepository categoryRepository,
        IPriorityRepository priorityRepository,
        IDepartmentRepository departmentRepository,
        ITicketStatusRepository ticketStatusRepository,
        ITicketSourceRepository ticketSourceRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.BusinessHour, Guid> businessHourRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.Holiday, Guid> holidayRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Sla.SlaPolicy, Guid> slaPolicyRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Tickets.Ticket, Guid> ticketRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Tickets.TicketActivity, Guid> ticketActivityRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.KnowledgeBase.KnowledgeArticle, Guid> articleRepository,
        Volo.Abp.Identity.IdentityRoleManager roleManager,
        Volo.Abp.Identity.IdentityUserManager userManager,
        Volo.Abp.PermissionManagement.IPermissionDataSeeder permissionDataSeeder,
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
        _ticketRepository = ticketRepository;
        _ticketActivityRepository = ticketActivityRepository;
        _articleRepository = articleRepository;
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDataSeeder = permissionDataSeeder;
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
        await SeedTicketsAsync();
        await SeedRolesAndUsersAsync();
        await SeedKnowledgeArticlesAsync();
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

    private async Task SeedTicketsAsync()
    {
        if (await _ticketRepository.GetCountAsync() > 0) return;

        var categories = await _categoryRepository.GetListAsync();
        var priorities = await _priorityRepository.GetListAsync();
        var statuses = await _ticketStatusRepository.GetListAsync();
        var sources = await _ticketSourceRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var defaultSlaPolicy = (await _slaPolicyRepository.GetListAsync()).FirstOrDefault(p => p.IsDefault);

        if (categories.Count == 0 || priorities.Count == 0 || statuses.Count == 0 || sources.Count == 0) return;

        var hw = categories.FirstOrDefault(c => c.Code == "HARDWARE") ?? categories[0];
        var sw = categories.FirstOrDefault(c => c.Code == "SOFTWARE") ?? categories[0];
        var net = categories.FirstOrDefault(c => c.Code == "NETWORK") ?? categories[0];
        var acc = categories.FirstOrDefault(c => c.Code == "ACCOUNT") ?? categories[0];
        var gen = categories.FirstOrDefault(c => c.Code == "GENERAL") ?? categories[0];

        var pCritical = priorities.FirstOrDefault(p => p.Code == "CRITICAL") ?? priorities[0];
        var pHigh = priorities.FirstOrDefault(p => p.Code == "HIGH") ?? priorities[0];
        var pMed = priorities.FirstOrDefault(p => p.Code == "MEDIUM") ?? priorities[0];
        var pLow = priorities.FirstOrDefault(p => p.Code == "LOW") ?? priorities[0];

        var sNew = statuses.FirstOrDefault(s => s.Code == "NEW") ?? statuses[0];
        var sAssigned = statuses.FirstOrDefault(s => s.Code == "ASSIGNED") ?? statuses[0];
        var sInProgress = statuses.FirstOrDefault(s => s.Code == "IN_PROGRESS") ?? statuses[0];
        var sResolved = statuses.FirstOrDefault(s => s.Code == "RESOLVED") ?? statuses[0];
        var sClosed = statuses.FirstOrDefault(s => s.Code == "CLOSED") ?? statuses[0];
        var sReopened = statuses.FirstOrDefault(s => s.Code == "REOPENED") ?? statuses[0];

        var srcEmail = sources.FirstOrDefault(s => s.Code == "EMAIL") ?? sources[0];
        var srcWeb = sources.FirstOrDefault(s => s.Code == "WEB_PORTAL") ?? sources[0];
        var srcPhone = sources.FirstOrDefault(s => s.Code == "PHONE") ?? sources[0];

        var deptIt = departments.FirstOrDefault(d => d.Code == "IT_SUPPORT") ?? departments.FirstOrDefault();

        var sampleTickets = new List<(string Number, string Title, string Desc, Guid Cat, Guid Prio, Guid Stat, Guid Src, string Name, string Email, DateTime Created, DateTime? Due, DateTime? Resolved, DateTime? Closed, string? Tags)>
        {
            ("TK-202609-0001", "Lỗi kết nối Wi-Fi phòng họp tầng 3", "Mạng Wi-Fi tại phòng họp lớn tầng 3 bị rớt liên tục trong các cuộc họp trực tuyến.", net.Id, pCritical.Id, sInProgress.Id, srcWeb.Id, "Nguyễn Văn A", "nguyenvana@company.com", DateTime.UtcNow.AddHours(-10), DateTime.UtcNow.AddHours(2), null, null, "Wifi,Network"),
            ("TK-202609-0002", "Máy in HP không nhận lệnh in từ phòng Kế Toán", "Máy in HP LaserJet 2035 bị kẹt giấy và phát tín hiệu cảnh báo đèn đỏ.", hw.Id, pHigh.Id, sAssigned.Id, srcPhone.Id, "Trần Thị B", "tranthib@company.com", DateTime.UtcNow.AddHours(-24), DateTime.UtcNow.AddHours(4), null, null, "Printer,Hardware"),
            ("TK-202609-0003", "Cấp lại mật khẩu tài khoản Misa Kế toán", "Nhân viên quên mật khẩu phần mềm kế toán Misa sau đợt nghỉ lễ.", acc.Id, pLow.Id, sResolved.Id, srcEmail.Id, "Lê Văn C", "levanc@company.com", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(-5), null, "Account,Misa"),
            ("TK-202609-0004", "Yêu cầu cài đặt bản quyền Adobe Photoshop 2026", "Phòng Marketing cần nâng cấp và kích hoạt bản quyền phần mềm thiết kế.", sw.Id, pMed.Id, sClosed.Id, srcWeb.Id, "Phạm Hoàng D", "phamhoangd@company.com", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-4), DateTime.UtcNow.AddDays(-3), "Software,Adobe"),
            ("TK-202609-0005", "Màn hình PC làm việc bị xọc xanh không lên hình", "Màn hình Dell Ultrasharp 27 inch bị hiện tượng chớp tắt liên tục.", hw.Id, pHigh.Id, sNew.Id, srcPhone.Id, "Hoàng Văn E", "hoangvane@company.com", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(10), null, null, "Hardware,Monitor"),
            ("TK-202609-0006", "Lỗi VPN Fortinet không kết nối được từ xa", "Nhân viên làm việc tại nhà (WFH) gặp lỗi 98 khi quay số VPN.", net.Id, pCritical.Id, sNew.Id, srcEmail.Id, "Đoàn Thị F", "doanthif@company.com", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(3), null, null, "VPN,Security"),
            ("TK-202609-0007", "Hỗ trợ xuất báo cáo tài chính cuối quý", "Cần trích xuất dữ liệu giao dịch hệ thống theo định dạng Excel.", gen.Id, pMed.Id, sResolved.Id, srcWeb.Id, "Vũ Văn G", "vuvang@company.com", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1), null, "Report,Finance"),
            ("TK-202609-0008", "Xin cấp quyền truy cập Folder Shared Drive Marketing", "Nhiệm vụ mới yêu cầu truy cập thư mục truyền thông thương hiệu.", acc.Id, pMed.Id, sAssigned.Id, srcWeb.Id, "Bùi Thị H", "buithih@company.com", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(12), null, null, "Permission,Drive"),
            ("TK-202609-0009", "Outlook 365 không gửi được email dung lượng lớn", "Thông báo lỗi vượt quá 25MB khi đính kèm tài liệu thuyết trình.", sw.Id, pHigh.Id, sReopened.Id, srcEmail.Id, "Ngô Văn I", "ngovani@company.com", DateTime.UtcNow.AddDays(-4), DateTime.UtcNow.AddDays(-2), null, null, "Outlook,Email"),
            ("TK-202609-0010", "Bảo trì định kỳ hệ thống máy chủ Server DB01", "Kế hoạch nâng cấp dung lượng lưu trữ ổ cứng SSD cho Server dữ liệu.", gen.Id, pLow.Id, sClosed.Id, srcWeb.Id, "Quản Trị Viên", "admin@company.com", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-6), DateTime.UtcNow.AddDays(-5), "Server,Maintenance")
        };

        foreach (var t in sampleTickets)
        {
            var ticket = new Helpdesk.Tickets.Ticket(
                _guidGenerator.Create(),
                t.Number,
                t.Title,
                t.Desc,
                t.Cat,
                t.Prio,
                t.Stat,
                t.Src,
                t.Name,
                t.Email,
                departmentId: deptIt?.Id,
                dueDate: t.Due,
                tags: t.Tags
            );

            ticket.SlaPolicyId = defaultSlaPolicy?.Id;
            ticket.ResolvedAt = t.Resolved;
            ticket.ClosedAt = t.Closed;
            ticket.FirstRespondedAt = t.Created.AddMinutes(30);

            await _ticketRepository.InsertAsync(ticket);

            await _ticketActivityRepository.InsertAsync(new Helpdesk.Tickets.TicketActivity(
                _guidGenerator.Create(),
                ticket.Id,
                Helpdesk.Tickets.TicketActivityType.Created,
                description: $"Yêu cầu hỗ trợ '{ticket.Title}' đã được khởi tạo bởi {ticket.RequesterName}"
            ));
        }
    }

    private async Task SeedRolesAndUsersAsync()
    {
        // 1. Roles & Permissions mapping
        var managerPermissions = new List<string>
        {
            "Helpdesk.Dashboard",
            "Helpdesk.Tickets", "Helpdesk.Tickets.Create", "Helpdesk.Tickets.Edit", "Helpdesk.Tickets.Delete", "Helpdesk.Tickets.Assign", "Helpdesk.Tickets.ChangeStatus", "Helpdesk.Tickets.AddComment",
            "Helpdesk.Sla", "Helpdesk.Sla.Policies", "Helpdesk.Sla.BusinessHours", "Helpdesk.Sla.Reports",
            "Helpdesk.Categories", "Helpdesk.Categories.Create", "Helpdesk.Categories.Edit", "Helpdesk.Categories.Delete",
            "Helpdesk.Priorities", "Helpdesk.Priorities.Create", "Helpdesk.Priorities.Edit", "Helpdesk.Priorities.Delete",
            "Helpdesk.Departments", "Helpdesk.Departments.Create", "Helpdesk.Departments.Edit", "Helpdesk.Departments.Delete",
            "Helpdesk.TicketStatuses", "Helpdesk.TicketStatuses.Create", "Helpdesk.TicketStatuses.Edit", "Helpdesk.TicketStatuses.Delete",
            "Helpdesk.TicketSources", "Helpdesk.TicketSources.Create", "Helpdesk.TicketSources.Edit", "Helpdesk.TicketSources.Delete",
            "Helpdesk.CannedResponses", "Helpdesk.CannedResponses.Create", "Helpdesk.CannedResponses.Edit", "Helpdesk.CannedResponses.Delete",
            "Helpdesk.KnowledgeBase", "Helpdesk.KnowledgeBase.Create", "Helpdesk.KnowledgeBase.Edit", "Helpdesk.KnowledgeBase.Delete", "Helpdesk.KnowledgeBase.Manage",
            "Helpdesk.CustomerPortal", "Helpdesk.CustomerPortal.CreateTicket",
            "AbpIdentity.Users"
        };

        var agentPermissions = new List<string>
        {
            "Helpdesk.Dashboard",
            "Helpdesk.Tickets", "Helpdesk.Tickets.Create", "Helpdesk.Tickets.Edit", "Helpdesk.Tickets.Assign", "Helpdesk.Tickets.ChangeStatus", "Helpdesk.Tickets.AddComment",
            "Helpdesk.CannedResponses", "Helpdesk.CannedResponses.Create", "Helpdesk.CannedResponses.Edit",
            "Helpdesk.KnowledgeBase", "Helpdesk.KnowledgeBase.Create", "Helpdesk.KnowledgeBase.Edit",
            "Helpdesk.CustomerPortal",
            "Helpdesk.Categories",
            "Helpdesk.Priorities",
            "Helpdesk.Departments",
            "Helpdesk.TicketStatuses",
            "Helpdesk.TicketSources",
            "AbpIdentity.Users"
        };

        var customerPermissions = new List<string>
        {
            "Helpdesk.CustomerPortal", "Helpdesk.CustomerPortal.CreateTicket",
            "Helpdesk.KnowledgeBase",
            "Helpdesk.Categories",
            "Helpdesk.Priorities",
            "Helpdesk.TicketStatuses",
            "Helpdesk.TicketSources"
        };

        // Ensure admin has all permissions
        var allPermissions = managerPermissions.Union(agentPermissions).Union(customerPermissions).Distinct().ToList();
        await _permissionDataSeeder.SeedAsync("R", "admin", allPermissions);

        // Create Roles with pre-assigned permissions
        await CreateRoleWithPermissionsAsync("HelpdeskManager", managerPermissions);
        await CreateRoleWithPermissionsAsync("SupportAgent", agentPermissions);
        await CreateRoleWithPermissionsAsync("Customer", customerPermissions);

        // 2. Create Sample Users for testing
        await CreateUserAsync("manager", "manager@helpdesk.com", "Quản Lý", "Trần", "Huy123@", "HelpdeskManager");
        await CreateUserAsync("agent1", "agent1@helpdesk.com", "Kỹ Thuật 1", "Nguyễn", "Huy123@", "SupportAgent");
        await CreateUserAsync("agent2", "agent2@helpdesk.com", "Kỹ Thuật 2", "Lê", "Huy123@", "SupportAgent");
        await CreateUserAsync("customer", "customer@company.com", "Khách Hàng", "Phạm", "Huy123@", "Customer");
    }

    private async Task SeedKnowledgeArticlesAsync()
    {
        if (await _articleRepository.GetCountAsync() > 0) return;

        var categories = await _categoryRepository.GetListAsync();
        if (categories.Count == 0) return;

        var net = categories.FirstOrDefault(c => c.Code == "NETWORK") ?? categories[0];
        var acc = categories.FirstOrDefault(c => c.Code == "ACCOUNT") ?? categories[0];
        var hw = categories.FirstOrDefault(c => c.Code == "HARDWARE") ?? categories[0];
        var sw = categories.FirstOrDefault(c => c.Code == "SOFTWARE") ?? categories[0];
        var gen = categories.FirstOrDefault(c => c.Code == "GENERAL") ?? categories[0];

        var articles = new List<Helpdesk.KnowledgeBase.KnowledgeArticle>
        {
            new Helpdesk.KnowledgeBase.KnowledgeArticle(
                _guidGenerator.Create(),
                "Hướng dẫn cài đặt và kết nối VPN FortiClient từ xa",
                "huong-dan-cai-dat-va-ket-noi-vpn-forticlient-tu-xa",
                net.Id,
                "### 1. Chuẩn bị thông tin kết nối\n- **Địa chỉ máy chủ VPN:** `vpn.company.com`\n- **Cổng kết nối:** `10443`\n- **Tên đăng nhập:** Tài khoản email công ty (bỏ phần @company.com)\n\n### 2. Các bước cài đặt\n1. Tải phần mềm FortiClient VPN phiên bản 7.2 từ trang chủ hoặc kho phần mềm nội bộ.\n2. Chạy tệp cài đặt và làm theo hướng dẫn trên màn hình.\n3. Khởi động lại máy tính nếu được yêu cầu.\n\n### 3. Cấu hình kết nối\n1. Mở ứng dụng FortiClient, chọn **Config VPN**.\n2. Chọn loại kết nối: **SSL-VPN**.\n3. Điền Tên kết nối: `Company VPN`, Remote Gateway: `vpn.company.com:10443`.\n4. Bấm **Save** và đăng nhập bằng tài khoản và mật khẩu của bạn.\n5. Nhập mã xác thực OTP từ ứng dụng Google Authenticator nếu được kích hoạt 2FA.",
                "Hướng dẫn chi tiết từng bước tải, cài đặt và cấu hình VPN FortiClient giúp nhân viên kết nối mạng nội bộ từ xa an toàn.",
                "VPN, Remote, Fortinet, Network",
                true
            ),
            new Helpdesk.KnowledgeBase.KnowledgeArticle(
                _guidGenerator.Create(),
                "Quy trình tự khôi phục mật khẩu tài khoản nội bộ (Self-Service Password Reset)",
                "quy-trinh-tu-khoi-phuc-mat-khau-tai-khoan-noi-bo",
                acc.Id,
                "### Trường hợp 1: Bạn vẫn đăng nhập được vào máy tính\n1. Nhấn tổ hợp phím `Ctrl + Alt + Delete`.\n2. Chọn **Đổi mật khẩu (Change a password)**.\n3. Nhập mật khẩu cũ và nhập mật khẩu mới 2 lần.\n*Lưu ý: Mật khẩu mới phải có tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.*\n\n### Trường hợp 2: Bạn quên mật khẩu hoàn toàn\n1. Truy cập cổng khôi phục tài khoản: `https://password.company.com`.\n2. Nhập email nhân viên công ty của bạn.\n3. Chọn phương thức xác thực qua SMS số điện thoại đã đăng ký với phòng Nhân Sự.\n4. Nhập mã OTP gồm 6 chữ số được gửi về điện thoại và tiến hành đặt lại mật khẩu mới.",
                "Cách đặt lại mật khẩu tài khoản hệ thống khi bị quên hoặc hết hạn định kỳ 90 ngày mà không cần chờ IT can thiệp thủ công.",
                "Password, Account, Reset, Security",
                true
            ),
            new Helpdesk.KnowledgeBase.KnowledgeArticle(
                _guidGenerator.Create(),
                "Khắc phục sự cố máy in văn phòng (Kẹt giấy, Offline, Không nhận lệnh in)",
                "khac-phuc-su-co-may-in-van-phong",
                hw.Id,
                "### 1. Máy in hiển thị trạng thái Offline hoặc Paused\n- Kiểm tra dây mạng LAN cắm sau lưng máy in xem đèn có nhấp nháy xanh không.\n- Vào **Settings > Bluetooth & devices > Printers & scanners**.\n- Nhấp vào máy in đang dùng, chọn **Open print queue**, mở menu **Printer** và bỏ chọn dòng **Use Printer Offline**.\n\n### 2. Máy in báo kẹt giấy (Paper Jam)\n- Tắt nguồn máy in bằng công tắc phía trước.\n- Mở nắp khay lấy giấy và nắp khoang chứa hộp mực (cartridge).\n- Dùng hai tay kéo nhẹ nhàng tờ giấy bị kẹt theo chiều thoát giấy thông thường (tránh giật mạnh làm rách vụn giấy kẹt lại trong trục cuốn).\n- Đóng nắp máy in cẩn thận và bật nguồn lại.",
                "Tổng hợp các bước xử lý nhanh lỗi máy in không in được, kẹt giấy hoặc máy in bị chuyển sang chế độ Offline.",
                "Printer, Hardware, May in, Paper Jam",
                true
            ),
            new Helpdesk.KnowledgeBase.KnowledgeArticle(
                _guidGenerator.Create(),
                "Hướng dẫn thiết lập hòm thư Outlook Microsoft 365 trên máy tính và điện thoại",
                "huong-dan-thiet-lap-hom-thu-outlook-microsoft-365",
                sw.Id,
                "### Cấu hình trên máy tính Windows (Outlook Desktop)\n1. Mở ứng dụng **Outlook** trên máy tính.\n2. Nếu là lần đầu mở, hộp thoại đăng nhập sẽ xuất hiện. Nhập địa chỉ email công ty dạng `ten.ho@company.com`.\n3. Bấm **Connect**.\n4. Trình duyệt xác thực tài khoản Microsoft 365 sẽ hiện ra, nhập mật khẩu và hoàn tất 2FA.\n5. Bỏ tích ô 'Cho phép tổ chức quản lý thiết bị của tôi' nếu đây là máy tính cá nhân (BYOD), rồi bấm **OK**.\n\n### Cấu hình trên Smartphone (iOS / Android)\n1. Tải ứng dụng chính thức **Microsoft Outlook** từ App Store hoặc Google Play.\n2. Chọn **Thêm tài khoản** và nhập email công ty.\n3. Đăng nhập và chấp nhận quyền thông báo để nhận email kịp thời.",
                "Hướng dẫn đăng nhập và đồng bộ email công ty Microsoft 365 trên máy tính cá nhân và thiết bị di động thông minh.",
                "Outlook, Email, Microsoft 365, Setup",
                true
            ),
            new Helpdesk.KnowledgeBase.KnowledgeArticle(
                _guidGenerator.Create(),
                "Quy trình xin cấp phát và bàn giao thiết bị CNTT (Laptop, Màn hình, Bàn phím)",
                "quy-trinh-xin-cap-phat-va-ban-giao-thiet-bi-cntt",
                gen.Id,
                "### 1. Đối tượng áp dụng\n- Nhân viên mới gia nhập công ty (Onboarding).\n- Nhân viên có nhu cầu nâng cấp hoặc đổi thiết bị do hư hỏng / khấu hao quá hạn.\n\n### 2. Các bước yêu cầu\n1. Tạo yêu cầu (Ticket) trên **Customer Portal** thuộc danh mục `General` hoặc `Hardware`.\n2. Ghi rõ lý do và đính kèm phê duyệt từ Trưởng bộ phận (Manager Approval) qua email hoặc văn bản.\n3. Bộ phận IT Helpdesk sẽ tiếp nhận, kiểm tra tồn kho và phản hồi thời gian hẹn bàn giao trong vòng 24 giờ làm việc.\n4. Khi nhận máy, nhân viên kiểm tra tình trạng vật lý, ký biên bản bàn giao thiết bị tài sản công ty.",
                "Quy chuẩn và các bước phê duyệt cần thiết khi nhân viên xin cấp mới laptop, màn hình rời hoặc phụ kiện công nghệ.",
                "Hardware, Equipment, Onboarding, IT Asset",
                true
            )
        };

        foreach (var art in articles)
        {
            art.Vote(true);
            art.Vote(true);
            art.Vote(true);
            art.IncrementViewCount();
            art.IncrementViewCount();
            art.IncrementViewCount();
            art.IncrementViewCount();
            art.IncrementViewCount();
            await _articleRepository.InsertAsync(art);
        }
    }

    private async Task CreateRoleWithPermissionsAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new Volo.Abp.Identity.IdentityRole(_guidGenerator.Create(), roleName)
            {
                IsPublic = true
            };
            await _roleManager.CreateAsync(role);
        }

        await _permissionDataSeeder.SeedAsync("R", roleName, permissions);
    }

    private async Task CreateUserAsync(string userName, string email, string name, string surname, string password, string roleName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new Volo.Abp.Identity.IdentityUser(_guidGenerator.Create(), userName, email)
            {
                Name = name,
                Surname = surname
            };
            user.SetEmailConfirmed(true);
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
