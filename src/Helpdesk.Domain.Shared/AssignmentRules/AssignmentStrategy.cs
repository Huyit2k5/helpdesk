namespace Helpdesk.AssignmentRules;

/// <summary>
/// Chiến lược phân bổ sự vụ cho kỹ thuật viên.
/// </summary>
public enum AssignmentStrategy
{
    /// <summary>
    /// Xoay vòng lần lượt giữa các kỹ thuật viên.
    /// </summary>
    RoundRobin = 1,

    /// <summary>
    /// Phân bổ cho kỹ thuật viên đang có số lượng vé chưa hoàn thành ít nhất (Cân bằng tải).
    /// </summary>
    LeastBusy = 2,

    /// <summary>
    /// Chỉ định cố định cho một kỹ thuật viên cụ thể.
    /// </summary>
    DirectAssign = 3
}
