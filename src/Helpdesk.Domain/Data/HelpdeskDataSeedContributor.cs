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
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Automations.AutomationRule, Guid> _automationRuleRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Automations.Macro, Guid> _macroRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Assets.Asset, Guid> _assetRepository;
    private readonly Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Assets.AssetActivity, Guid> _assetActivityRepository;
    private readonly Volo.Abp.Identity.IdentityRoleManager _roleManager;
    private readonly Volo.Abp.Identity.IdentityUserManager _userManager;
    private readonly Volo.Abp.PermissionManagement.IPermissionDataSeeder _permissionDataSeeder;
    private readonly Volo.Abp.Authorization.Permissions.IPermissionDefinitionManager _permissionDefinitionManager;

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
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Automations.AutomationRule, Guid> automationRuleRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Automations.Macro, Guid> macroRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Assets.Asset, Guid> assetRepository,
        Volo.Abp.Domain.Repositories.IRepository<Helpdesk.Assets.AssetActivity, Guid> assetActivityRepository,
        Volo.Abp.Identity.IdentityRoleManager roleManager,
        Volo.Abp.Identity.IdentityUserManager userManager,
        Volo.Abp.PermissionManagement.IPermissionDataSeeder permissionDataSeeder,
        Volo.Abp.Authorization.Permissions.IPermissionDefinitionManager permissionDefinitionManager,
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
        _automationRuleRepository = automationRuleRepository;
        _macroRepository = macroRepository;
        _assetRepository = assetRepository;
        _assetActivityRepository = assetActivityRepository;
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDataSeeder = permissionDataSeeder;
        _permissionDefinitionManager = permissionDefinitionManager;
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
        await SeedAssetsAsync();
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

    private async Task SeedAssetsAsync()
    {
        if (await _assetRepository.GetCountAsync() > 0)
        {
            return;
        }

        var itDept = await _departmentRepository.FindAsync(d => d.Code == "IT");
        var hrDept = await _departmentRepository.FindAsync(d => d.Code == "HR");
        var accDept = await _departmentRepository.FindAsync(d => d.Code == "ACC");

        // 1. MacBook Pro 16" M3 Max
        var asset1 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0001",
            "MacBook Pro 16\" M3 Max",
            Helpdesk.Assets.AssetType.Laptop,
            Helpdesk.Assets.AssetStatus.Assigned,
            serialNumber: "C02G1234MD6R",
            model: "MacBook Pro 16\" (M3 Max / 36GB / 1TB)",
            manufacturer: "Apple Inc.",
            location: "Tầng 3 - Phòng Kỹ Thuật (IT Dept)",
            purchaseDate: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 72000000m,
            specifications: "CPU: Apple M3 Max 14-core\nRAM: 36GB Unified Memory\nSSD: 1TB NVMe\nMàn hình: 16.2\" Liquid Retina XDR 120Hz ProMotion\nOS: macOS Sonoma 14.5",
            notes: "Máy cấp phát cho Trưởng nhóm kỹ thuật Lead Developer."
        );
        asset1.AssignTo(null, "Quản Trị Viên (admin)", "admin@company.com", itDept?.Name ?? "Phòng Công Nghệ Thông Tin", new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc));
        await _assetRepository.InsertAsync(asset1);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset1.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Nhập kho thiết bị MacBook Pro 16\" M3 Max mới 100%."
        ));
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset1.Id,
            Helpdesk.Assets.AssetActivityType.Assigned,
            "Cấp phát thiết bị",
            "Bàn giao máy cho Quản Trị Viên (admin) sử dụng phát triển hệ thống."
        ));

        // 2. Dell Latitude 5540
        var asset2 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0002",
            "Dell Latitude 5540",
            Helpdesk.Assets.AssetType.Laptop,
            Helpdesk.Assets.AssetStatus.Assigned,
            serialNumber: "8H7F9K2",
            model: "Latitude 5540 (Core i7-1365U / 16GB / 512GB)",
            manufacturer: "Dell Inc.",
            location: "Tầng 2 - Phòng Kế Toán",
            purchaseDate: new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2027, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 26500000m,
            specifications: "CPU: Intel Core i7-1365U vPro (10 Cores, 12 Threads)\nRAM: 16GB DDR5 5200MHz\nSSD: 512GB PCIe NVMe Gen4\nMàn hình: 15.6\" FHD IPS Anti-Glare\nOS: Windows 11 Pro 64-bit",
            notes: "Đã cài đặt sẵn phần mềm kế toán Misa SME và Office 365 bản quyền."
        );
        asset2.AssignTo(null, "Nguyễn Thị Hoa", "hoant@company.com", accDept?.Name ?? "Phòng Kế Toán", new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc));
        await _assetRepository.InsertAsync(asset2);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset2.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Nhập kho thiết bị Dell Latitude 5540."
        ));
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset2.Id,
            Helpdesk.Assets.AssetActivityType.Assigned,
            "Cấp phát thiết bị",
            "Bàn giao cho Kế toán viên Nguyễn Thị Hoa phục vụ công việc quyết toán."
        ));

        // 3. Màn hình Dell UltraSharp 27" 4K (U2723QE)
        var asset3 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0003",
            "Màn hình Dell UltraSharp 27\" 4K (U2723QE)",
            Helpdesk.Assets.AssetType.Monitor,
            Helpdesk.Assets.AssetStatus.InStock,
            serialNumber: "CN-0V2F11-74445",
            model: "UltraSharp U2723QE 4K IPS Black USB-C Hub",
            manufacturer: "Dell Inc.",
            location: "Kho IT - Kệ A2",
            purchaseDate: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2029, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 14200000m,
            specifications: "Kích thước: 27 inch IPS Black Technology\nĐộ phân giải: 4K UHD (3840 x 2160) @ 60Hz\nĐộ tương phản: 2000:1\nCổng kết nối: USB-C 90W PD, RJ45 Ethernet, DisplayPort 1.4, HDMI 2.0",
            notes: "Màn hình dự phòng trong kho sẵn sàng cấp phát cho nhân sự thiết kế đồ họa."
        );
        await _assetRepository.InsertAsync(asset3);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset3.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Nhập kho lưu trữ 01 màn hình Dell UltraSharp 4K mới nguyên hộp."
        ));

        // 4. Cisco Catalyst 1000 24-Port Switch
        var asset4 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0004",
            "Cisco Catalyst 1000 24-Port PoE+ Switch",
            Helpdesk.Assets.AssetType.NetworkDevice,
            Helpdesk.Assets.AssetStatus.Assigned,
            serialNumber: "FOC2438V01A",
            model: "C9200L-24P-4G-E",
            manufacturer: "Cisco Systems",
            location: "Phòng Server Trung Tâm - Tủ Rack 01",
            purchaseDate: new DateTime(2025, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2028, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 38500000m,
            specifications: "Cổng: 24x 10/100/1000 Ethernet PoE+ ports (370W PoE budget)\nUplink: 4x 1G SFP uplinks\nQuản trị: Web UI, Cisco IOS CLI, SNMPv3, SSH",
            notes: "Thiết bị switch trục chính cung cấp mạng tầng 1 và tầng 2 kèm cấp nguồn cho camera & Access Point."
        );
        asset4.AssignTo(null, "Đội Quản Trị Mạng & Server", "network-admin@company.com", itDept?.Name ?? "Phòng Công Nghệ Thông Tin", new DateTime(2025, 11, 25, 0, 0, 0, DateTimeKind.Utc));
        await _assetRepository.InsertAsync(asset4);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset4.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Đưa thiết bị Switch Cisco vào hệ thống giám sát hạ tầng."
        ));

        // 5. HP LaserJet Pro MFP M428fdw
        var asset5 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0005",
            "Máy In Đa Năng HP LaserJet Pro MFP M428fdw",
            Helpdesk.Assets.AssetType.PrinterPeripheral,
            Helpdesk.Assets.AssetStatus.UnderRepair,
            serialNumber: "VNB3K18492",
            model: "LaserJet Pro MFP M428fdw",
            manufacturer: "HP Inc.",
            location: "Tầng 1 - Khu Lễ Tân & Hành Chính",
            purchaseDate: new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 12800000m,
            specifications: "Chức năng: In, Scan, Copy, Fax hai mặt tự động\nTốc độ: 38 trang/phút\nĐộ phân giải: 1200 x 1200 dpi\nKết nối: Wi-Fi Dual-Band, Gigabit Ethernet, USB 2.0",
            notes: "Máy in lễ tân tầng 1, đang báo lỗi kẹt giấy liên tục và đang gửi trung tâm bảo hành HP ủy quyền."
        );
        asset5.AssignTo(null, "Bộ Phận Lễ Tân - Hành Chính", "letan@company.com", hrDept?.Name ?? "Phòng Hành Chính Nhân Sự", new DateTime(2025, 8, 20, 0, 0, 0, DateTimeKind.Utc));
        asset5.ChangeStatus(Helpdesk.Assets.AssetStatus.UnderRepair);
        await _assetRepository.InsertAsync(asset5);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset5.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Nhập kho và cấu hình máy in đa năng HP LaserJet."
        ));
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset5.Id,
            Helpdesk.Assets.AssetActivityType.SentToRepair,
            "Gửi đi bảo dưỡng / Sửa chữa",
            "Gửi máy in sang TTBH HP Việt Nam kiểm tra lỗi kẹt giấy và thay cụm sấy (Fuser Unit)."
        ));

        // 6. ThinkPad X1 Carbon Gen 11
        var asset6 = new Helpdesk.Assets.Asset(
            _guidGenerator.Create(),
            "AST-20260901-0006",
            "ThinkPad X1 Carbon Gen 11",
            Helpdesk.Assets.AssetType.Laptop,
            Helpdesk.Assets.AssetStatus.InStock,
            serialNumber: "PF4G78M1",
            model: "ThinkPad X1 Carbon Gen 11 (Core i7-1370P / 32GB / 1TB)",
            manufacturer: "Lenovo",
            location: "Kho IT - Tủ bảo mật S3",
            purchaseDate: new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            warrantyExpiryDate: new DateTime(2029, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            purchaseCost: 48000000m,
            specifications: "CPU: Intel Core i7-1370P vPro (14 Cores, 20 Threads)\nRAM: 32GB LPDDR5 6000MHz\nSSD: 1TB PCIe NVMe Gen4 Performance\nMàn hình: 14\" 2.8K OLED (2880x1800) HDR 500 True Black\nTrọng lượng: 1.12 kg\nOS: Windows 11 Pro 64-bit",
            notes: "Thiết bị siêu mỏng nhẹ cao cấp dành cho Ban Giám Đốc hoặc đi công tác nước ngoài."
        );
        await _assetRepository.InsertAsync(asset6);
        await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
            _guidGenerator.Create(),
            asset6.Id,
            Helpdesk.Assets.AssetActivityType.Created,
            "Khởi tạo tài sản",
            "Nhập kho thiết bị ThinkPad X1 Carbon Gen 11 tình trạng Mới 100%."
        ));

        // Liên kết máy in HP LaserJet Pro MFP M428fdw vào ticket sự cố máy in
        var printerTicket = await _ticketRepository.FindAsync(t => t.TicketNumber.Contains("0002") || t.Title.Contains("máy in") || t.Title.Contains("Máy in"));
        if (printerTicket != null)
        {
            printerTicket.AssetId = asset5.Id;
            await _ticketRepository.UpdateAsync(printerTicket);

            await _assetActivityRepository.InsertAsync(new Helpdesk.Assets.AssetActivity(
                _guidGenerator.Create(),
                asset5.Id,
                Helpdesk.Assets.AssetActivityType.TicketLinked,
                $"Liên kết sự cố: {printerTicket.TicketNumber}",
                printerTicket.Title,
                relatedTicketId: printerTicket.Id
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
            "Helpdesk.Assets", "Helpdesk.Assets.Create", "Helpdesk.Assets.Edit", "Helpdesk.Assets.Delete", "Helpdesk.Assets.Assign", "Helpdesk.Assets.ChangeStatus",
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
            "Helpdesk.Assets", "Helpdesk.Assets.Create", "Helpdesk.Assets.Edit", "Helpdesk.Assets.Assign", "Helpdesk.Assets.ChangeStatus",
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

        // Ensure admin has all permissions. Dùng danh sách quyền tĩnh phía trên làm nền, cộng thêm TOÀN BỘ
        // quyền "Helpdesk.*" hiện có trong PermissionDefinitionProvider (kể cả các module thêm sau này như
        // AssignmentRules, DiscordSettings...) - tránh phải nhớ ra sửa tay + chạy SQL grant mỗi lần thêm module mới.
        var allHelpdeskPermissions = (await _permissionDefinitionManager.GetPermissionsAsync())
            .Where(p => p.Name.StartsWith("Helpdesk.", StringComparison.Ordinal))
            .Select(p => p.Name);

        var allPermissions = managerPermissions
            .Union(agentPermissions)
            .Union(customerPermissions)
            .Union(allHelpdeskPermissions)
            .Distinct()
            .ToList();
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

    private async Task SeedAutomationsAndMacrosAsync()
    {
        var priorities = await _priorityRepository.GetListAsync();
        var statuses = await _ticketStatusRepository.GetListAsync();

        var criticalPriority = priorities.FirstOrDefault(p => p.Name == "Critical") ?? priorities.FirstOrDefault();
        var pendingStatus = statuses.FirstOrDefault(s => s.Name == "Pending") ?? statuses.FirstOrDefault();
        var inProgressStatus = statuses.FirstOrDefault(s => s.Name == "In Progress") ?? statuses.FirstOrDefault();
        var resolvedStatus = statuses.FirstOrDefault(s => s.Name == "Resolved") ?? statuses.FirstOrDefault();
        var newStatus = statuses.FirstOrDefault(s => s.Name == "New") ?? statuses.FirstOrDefault();

        // Seed Automation Rules
        if (await _automationRuleRepository.GetCountAsync() == 0)
        {
            // Rule 1: Phát hiện từ khóa khẩn cấp khi tạo vé -> nâng Critical
            var rule1 = new Helpdesk.Automations.AutomationRule(
                _guidGenerator.Create(),
                "Cảnh Báo & Nâng Mức Khẩn Cấp Khi Có Từ Khóa Nguy Hiểm",
                Helpdesk.Automations.AutomationTriggerType.OnTicketCreated,
                executionOrder: 1,
                description: "Tự động nâng độ ưu tiên lên Critical và gắn nhãn cảnh báo khi tiêu đề chứa từ khóa nghiêm trọng",
                isActive: true
            );
            rule1.SetConditions(new List<Helpdesk.Automations.RuleCondition>
            {
                new() { Field = Helpdesk.Automations.ConditionField.Title, Operator = Helpdesk.Automations.ConditionOperator.Contains, Value = "sập server" }
            });
            rule1.SetActions(new List<Helpdesk.Automations.RuleAction>
            {
                new() { ActionType = Helpdesk.Automations.AutomationActionType.ChangePriority, TargetValue = criticalPriority?.Id.ToString() },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddTags, TargetValue = "Critical" },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddComment, TargetValue = "⚠️ [Tự Động Hóa] Hệ thống phát hiện sự cố nghiêm trọng qua từ khóa, tự động nâng độ ưu tiên lên Critical.", AdditionalValue = "true" },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.SendDiscordAlert, TargetValue = "Cảnh báo khẩn cấp!" }
            });
            await _automationRuleRepository.InsertAsync(rule1);

            // Rule 2: Tự động chuyển sang In Progress khi có phản hồi mới
            if (newStatus != null && inProgressStatus != null)
            {
                var rule2 = new Helpdesk.Automations.AutomationRule(
                    _guidGenerator.Create(),
                    "Tự Động Đổi Trạng Thái Sang Đang Xử Lý Khi Có Phản Hồi Mới",
                    Helpdesk.Automations.AutomationTriggerType.OnCommentAdded,
                    executionOrder: 2,
                    description: "Chuyển vé từ New sang In Progress ngay khi kỹ thuật viên hoặc khách hàng gửi bình luận",
                    isActive: true
                );
                rule2.SetConditions(new List<Helpdesk.Automations.RuleCondition>
                {
                    new() { Field = Helpdesk.Automations.ConditionField.Status, Operator = Helpdesk.Automations.ConditionOperator.Equals, Value = newStatus.Id.ToString() }
                });
                rule2.SetActions(new List<Helpdesk.Automations.RuleAction>
                {
                    new() { ActionType = Helpdesk.Automations.AutomationActionType.ChangeStatus, TargetValue = inProgressStatus.Id.ToString() },
                    new() { ActionType = Helpdesk.Automations.AutomationActionType.AddTags, TargetValue = "Active-Discussion" }
                });
                await _automationRuleRepository.InsertAsync(rule2);
            }

            // Rule 3: Đóng vé nhàn rỗi sau 48h
            if (pendingStatus != null && resolvedStatus != null)
            {
                var rule3 = new Helpdesk.Automations.AutomationRule(
                    _guidGenerator.Create(),
                    "Tự Động Đóng Sự Vụ Chờ Khách Hàng Sau 48 Giờ",
                    Helpdesk.Automations.AutomationTriggerType.ScheduledTime,
                    executionOrder: 3,
                    description: "Quét ngầm định kỳ: Tự động hoàn tất các vé Pending quá 48h không có tương tác",
                    isActive: true
                );
                rule3.SetConditions(new List<Helpdesk.Automations.RuleCondition>
                {
                    new() { Field = Helpdesk.Automations.ConditionField.Status, Operator = Helpdesk.Automations.ConditionOperator.Equals, Value = pendingStatus.Id.ToString() },
                    new() { Field = Helpdesk.Automations.ConditionField.HoursSinceLastUpdate, Operator = Helpdesk.Automations.ConditionOperator.GreaterThan, Value = "48" }
                });
                rule3.SetActions(new List<Helpdesk.Automations.RuleAction>
                {
                    new() { ActionType = Helpdesk.Automations.AutomationActionType.ChangeStatus, TargetValue = resolvedStatus.Id.ToString() },
                    new() { ActionType = Helpdesk.Automations.AutomationActionType.AddComment, TargetValue = "Sự vụ được tự động chuyển sang Resolved do khách hàng không phản hồi sau 48 giờ.", AdditionalValue = "false" },
                    new() { ActionType = Helpdesk.Automations.AutomationActionType.AddTags, TargetValue = "Auto-Closed" }
                });
                await _automationRuleRepository.InsertAsync(rule3);
            }
        }

        // Seed Macros
        if (await _macroRepository.GetCountAsync() == 0)
        {
            var macro1 = new Helpdesk.Automations.Macro(
                _guidGenerator.Create(),
                "Hướng Dẫn Reset Mật Khẩu",
                "Chèn hướng dẫn lấy lại mật khẩu, đổi trạng thái sang Pending và gán nhãn Password-Reset",
                order: 1,
                isActive: true
            );
            macro1.SetActions(new List<Helpdesk.Automations.RuleAction>
            {
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddComment, TargetValue = "Chào bạn,\n\nĐể thiết lập lại mật khẩu tài khoản của bạn, vui lòng thực hiện các bước sau:\n1. Truy cập trang đăng nhập và bấm 'Quên mật khẩu'.\n2. Nhập email doanh nghiệp để nhận mã xác nhận OTP.\n3. Tạo mật khẩu mới tối thiểu 8 ký tự.\n\nNếu cần hỗ trợ thêm, bạn hãy phản hồi lại vé này nhé!", AdditionalValue = "false" },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.ChangeStatus, TargetValue = pendingStatus?.Id.ToString() },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddTags, TargetValue = "Password-Reset" }
            });
            await _macroRepository.InsertAsync(macro1);

            var macro2 = new Helpdesk.Automations.Macro(
                _guidGenerator.Create(),
                "Đã Hỗ Trợ Từ Xa Xong (UltraViewer / TeamViewer)",
                "Chèn ghi chú nội bộ đã remote hỗ trợ và hoàn tất vé",
                order: 2,
                isActive: true
            );
            macro2.SetActions(new List<Helpdesk.Automations.RuleAction>
            {
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddComment, TargetValue = "Đã kết nối UltraViewer/TeamViewer vào máy người dùng để kiểm tra và xử lý dứt điểm sự cố.", AdditionalValue = "true" },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.ChangeStatus, TargetValue = resolvedStatus?.Id.ToString() },
                new() { ActionType = Helpdesk.Automations.AutomationActionType.AddTags, TargetValue = "Remote-Assisted" }
            });
            await _macroRepository.InsertAsync(macro2);
        }
    }
}

