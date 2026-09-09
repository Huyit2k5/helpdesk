using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.AssignmentRules;

/// <summary>
/// Quy tắc tự động điều phối và phân công vé cho kỹ thuật viên.
/// </summary>
public class AssignmentRule : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Thứ tự ưu tiên kiểm tra quy tắc (số nhỏ hơn ưu tiên trước).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Kích hoạt quy tắc hay không.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Chiến lược phân bổ (RoundRobin, LeastBusy, DirectAssign).
    /// </summary>
    public AssignmentStrategy RoutingStrategy { get; set; } = AssignmentStrategy.RoundRobin;

    /// <summary>
    /// Phòng ban điều phối đến (nếu có).
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Điều kiện: Danh mục sự cố (null = tất cả).
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Điều kiện: Mức độ ưu tiên (null = tất cả).
    /// </summary>
    public Guid? PriorityId { get; set; }

    /// <summary>
    /// Điều kiện: Kênh tiếp nhận (null = tất cả).
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Kỹ thuật viên chỉ định cố định (nếu chiến lược là DirectAssign).
    /// </summary>
    public Guid? DirectAssigneeId { get; set; }

    /// <summary>
    /// ID kỹ thuật viên vừa được phân công lần trước (dùng cho thuật toán Round-Robin).
    /// </summary>
    public Guid? LastAssignedUserId { get; set; }

    /// <summary>
    /// Danh sách kỹ thuật viên tham gia nhận vé theo quy tắc.
    /// </summary>
    public virtual ICollection<AssignmentRuleAgent> RuleAgents { get; private set; }

    protected AssignmentRule()
    {
        RuleAgents = new Collection<AssignmentRuleAgent>();
    }

    public AssignmentRule(
        Guid id,
        string name,
        int order = 0,
        AssignmentStrategy routingStrategy = AssignmentStrategy.RoundRobin,
        string? description = null,
        Guid? departmentId = null,
        Guid? categoryId = null,
        Guid? priorityId = null,
        Guid? sourceId = null,
        Guid? directAssigneeId = null,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        Order = order;
        RoutingStrategy = routingStrategy;
        Description = description;
        DepartmentId = departmentId;
        CategoryId = categoryId;
        PriorityId = priorityId;
        SourceId = sourceId;
        DirectAssigneeId = directAssigneeId;
        IsActive = isActive;
        RuleAgents = new Collection<AssignmentRuleAgent>();
    }

    public AssignmentRule SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), AssignmentRuleConsts.MaxNameLength);
        return this;
    }

    public void AddAgent(Guid agentId, Guid userId, int order = 0)
    {
        RuleAgents.Add(new AssignmentRuleAgent(agentId, Id, userId, order));
    }

    public void RemoveAgent(Guid userId)
    {
        var agent = ((List<AssignmentRuleAgent>)RuleAgents).Find(a => a.UserId == userId);
        if (agent != null)
        {
            RuleAgents.Remove(agent);
        }
    }

    public void ClearAgents()
    {
        RuleAgents.Clear();
    }
}
