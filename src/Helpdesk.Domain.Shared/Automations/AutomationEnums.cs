namespace Helpdesk.Automations;

/// <summary>
/// Thời điểm kích hoạt quy tắc tự động hóa.
/// </summary>
public enum AutomationTriggerType
{
    /// <summary>
    /// Kích hoạt tức thì khi một sự vụ mới được tạo.
    /// </summary>
    OnTicketCreated = 1,

    /// <summary>
    /// Kích hoạt khi sự vụ được cập nhật trạng thái, người phụ trách, v.v.
    /// </summary>
    OnTicketUpdated = 2,

    /// <summary>
    /// Kích hoạt khi có một bình luận/phản hồi mới được thêm vào sự vụ.
    /// </summary>
    OnCommentAdded = 3,

    /// <summary>
    /// Quét định kỳ theo lịch trình bởi Background Worker (Time-based).
    /// </summary>
    ScheduledTime = 4
}

/// <summary>
/// Trường dữ liệu để kiểm tra điều kiện.
/// </summary>
public enum ConditionField
{
    Status = 1,
    Priority = 2,
    Category = 3,
    Department = 4,
    Assignee = 5,
    Source = 6,
    Title = 7,
    Description = 8,
    HoursSinceLastUpdate = 9,
    HoursSinceCreated = 10,
    IsUnassigned = 11,
    Tags = 12
}

/// <summary>
/// Toán tử so sánh trong điều kiện.
/// </summary>
public enum ConditionOperator
{
    Equals = 1,
    NotEquals = 2,
    Contains = 3,
    NotContains = 4,
    GreaterThan = 5,
    LessThan = 6,
    IsEmpty = 7,
    IsNotEmpty = 8
}

/// <summary>
/// Loại hành động được tự động thực thi.
/// </summary>
public enum AutomationActionType
{
    ChangeStatus = 1,
    ChangePriority = 2,
    AssignToUser = 3,
    AssignToDepartment = 4,
    AddTags = 5,
    AddComment = 6,
    SendDiscordAlert = 7,
    SendEmail = 8
}
