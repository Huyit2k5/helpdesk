using System;

namespace Helpdesk.Automations;

/// <summary>
/// Đại diện cho một điều kiện kiểm tra trong quy tắc tự động hóa.
/// </summary>
public class RuleCondition
{
    public ConditionField Field { get; set; }
    public ConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// Đại diện cho một hành động được thực thi tự động.
/// </summary>
public class RuleAction
{
    public AutomationActionType ActionType { get; set; }

    /// <summary>
    /// Giá trị mục tiêu (ví dụ: Id trạng thái mới, Id độ ưu tiên mới, Id người dùng, Id phòng ban, hoặc nhãn tags).
    /// </summary>
    public string? TargetValue { get; set; }

    /// <summary>
    /// Giá trị bổ sung (ví dụ: nội dung bình luận, template thông báo, cờ nội bộ "true"/"false").
    /// </summary>
    public string? AdditionalValue { get; set; }
}
