using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Tickets;

/// <summary>
/// Thực thể Ticket trung tâm quản lý toàn bộ yêu cầu hỗ trợ.
/// </summary>
public class Ticket : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Mã định danh vé hiển thị (ví dụ: TK-202609-0001).
    /// </summary>
    public string TicketNumber { get; private set; } = null!;

    /// <summary>
    /// Tiêu đề yêu cầu hỗ trợ.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Mô tả chi tiết nội dung sự cố / dịch vụ cần hỗ trợ.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid StatusId { get; set; }

    public Guid SourceId { get; set; }

    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Nhân viên kỹ thuật / hỗ trợ phụ trách ticket này.
    /// </summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>
    /// Khách hàng / Nhân sự gửi yêu cầu (nếu đã có tài khoản hệ thống).
    /// </summary>
    public Guid? RequesterId { get; set; }

    public string RequesterName { get; set; } = string.Empty;

    public string RequesterEmail { get; set; } = string.Empty;

    public string? RequesterPhone { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public Guid? SlaPolicyId { get; set; }

    public DateTime? FirstResponseDueDate { get; set; }

    public DateTime? FirstRespondedAt { get; set; }

    public bool IsFirstResponseBreached { get; set; }

    public bool IsResolutionBreached { get; set; }

    public string? Tags { get; set; }

    protected Ticket()
    {
        // For EF Core
    }

    public Ticket(
        Guid id,
        string ticketNumber,
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
        : base(id)
    {
        SetTicketNumber(ticketNumber);
        SetTitle(title);
        Description = description ?? string.Empty;
        CategoryId = categoryId;
        PriorityId = priorityId;
        StatusId = statusId;
        SourceId = sourceId;
        DepartmentId = departmentId;
        AssigneeId = assigneeId;
        RequesterId = requesterId;
        RequesterName = Check.NotNullOrWhiteSpace(requesterName, nameof(requesterName), TicketConsts.MaxRequesterNameLength);
        RequesterEmail = Check.NotNullOrWhiteSpace(requesterEmail, nameof(requesterEmail), TicketConsts.MaxRequesterEmailLength);
        RequesterPhone = requesterPhone;
        DueDate = dueDate;
        Tags = tags;
    }

    public Ticket SetTicketNumber(string ticketNumber)
    {
        TicketNumber = Check.NotNullOrWhiteSpace(ticketNumber, nameof(ticketNumber), TicketConsts.MaxTicketNumberLength);
        return this;
    }

    public Ticket SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), TicketConsts.MaxTitleLength);
        return this;
    }
}
