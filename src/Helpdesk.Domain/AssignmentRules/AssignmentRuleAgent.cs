using System;
using Volo.Abp.Domain.Entities;

namespace Helpdesk.AssignmentRules;

/// <summary>
/// Nhân viên kỹ thuật tham gia tiếp nhận vé theo quy tắc phân công.
/// </summary>
public class AssignmentRuleAgent : Entity<Guid>
{
    public Guid RuleId { get; set; }

    /// <summary>
    /// ID kỹ thuật viên (IdentityUser).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Thứ tự xoay vòng trong danh sách.
    /// </summary>
    public int Order { get; set; }

    protected AssignmentRuleAgent()
    {
    }

    public AssignmentRuleAgent(Guid id, Guid ruleId, Guid userId, int order = 0)
        : base(id)
    {
        RuleId = ruleId;
        UserId = userId;
        Order = order;
    }
}
