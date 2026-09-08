using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.TicketStatuses;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Timing;

namespace Helpdesk.Tickets;

public class TicketManager : DomainService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IClock _clock;

    public TicketManager(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IClock clock)
    {
        _ticketRepository = ticketRepository;
        _activityRepository = activityRepository;
        _clock = clock;
    }

    /// <summary>
    /// Sinh mã vé tiếp theo dạng TK-yyyyMM-0001
    /// </summary>
    public async Task<string> GenerateTicketNumberAsync()
    {
        var now = _clock.Now;
        var prefix = $"TK-{now:yyyyMM}-";
        
        var queryable = await _ticketRepository.GetQueryableAsync();
        var countThisMonth = queryable.Count(t => t.TicketNumber.StartsWith(prefix));
        
        var nextSeq = countThisMonth + 1;
        var candidate = $"{prefix}{nextSeq:D4}";

        // Đảm bảo không trùng lặp nếu có concurrent request
        while (queryable.Any(t => t.TicketNumber == candidate))
        {
            nextSeq++;
            candidate = $"{prefix}{nextSeq:D4}";
        }

        return candidate;
    }

    /// <summary>
    /// Khởi tạo vé và ghi nhận vết khởi tạo ban đầu.
    /// </summary>
    public async Task<Ticket> CreateAsync(
        string title,
        string description,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        Guid sourceId,
        string requesterName,
        string requesterEmail,
        Guid? departmentId = null,
        Guid? assigneeId = null,
        Guid? requesterId = null,
        string? requesterPhone = null,
        DateTime? dueDate = null,
        string? tags = null)
    {
        var ticketNumber = await GenerateTicketNumberAsync();
        var ticketId = GuidGenerator.Create();

        var ticket = new Ticket(
            ticketId,
            ticketNumber,
            title,
            description,
            categoryId,
            priorityId,
            statusId,
            sourceId,
            requesterName,
            requesterEmail,
            departmentId,
            assigneeId,
            requesterId,
            requesterPhone,
            dueDate,
            tags
        );

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.Created,
            description: $"Vé đã được tạo với mã số {ticketNumber}."
        );

        await _activityRepository.InsertAsync(activity);

        return ticket;
    }

    /// <summary>
    /// Thay đổi trạng thái vé và cập nhật mốc thời gian liên quan.
    /// </summary>
    public async Task ChangeStatusAsync(Ticket ticket, TicketStatus oldStatus, TicketStatus newStatus)
    {
        if (ticket.StatusId == newStatus.Id)
        {
            return;
        }

        ticket.StatusId = newStatus.Id;

        if (newStatus.StatusGroup == StatusGroup.Closed || newStatus.IsFinal)
        {
            ticket.ResolvedAt ??= _clock.Now;
            ticket.ClosedAt = _clock.Now;
        }
        else if (newStatus.StatusGroup == StatusGroup.InProgress)
        {
            ticket.ClosedAt = null;
        }

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            newStatus.StatusGroup == StatusGroup.Closed ? TicketActivityType.Closed : TicketActivityType.StatusChanged,
            fieldName: "Status",
            oldVal: oldStatus.Name,
            newVal: newStatus.Name,
            description: $"Trạng thái vé đổi từ '{oldStatus.Name}' sang '{newStatus.Name}'."
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Phân công xử lý vé.
    /// </summary>
    public async Task AssignAsync(
        Ticket ticket,
        Guid? newAssigneeId,
        string? assigneeName,
        Guid? newDepartmentId,
        string? departmentName)
    {
        ticket.AssigneeId = newAssigneeId;
        if (newDepartmentId.HasValue)
        {
            ticket.DepartmentId = newDepartmentId;
        }

        var desc = !string.IsNullOrWhiteSpace(assigneeName)
            ? $"Vé được phân công cho {assigneeName}."
            : (!string.IsNullOrWhiteSpace(departmentName)
                ? $"Vé được phân công cho phòng ban {departmentName}."
                : "Vé đã được hủy phân công.");

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.Assigned,
            fieldName: "Assignee",
            newVal: assigneeName,
            description: desc
        );

        await _activityRepository.InsertAsync(activity);
    }
}
