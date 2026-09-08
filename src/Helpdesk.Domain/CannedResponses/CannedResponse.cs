using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.CannedResponses;

/// <summary>
/// Mẫu phản hồi nhanh, có thể gắn với danh mục cụ thể.
/// </summary>
public class CannedResponse : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Title { get; private set; } = null!;

    public string Content { get; set; } = null!;

    /// <summary>
    /// Danh mục liên quan (để gợi ý canned response phù hợp khi xử lý ticket).
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Nếu true, tất cả agent đều thấy. Nếu false, chỉ người tạo thấy.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Đếm lượt sử dụng để sắp xếp theo mức phổ biến.
    /// </summary>
    public int UsageCount { get; set; }

    protected CannedResponse()
    {
    }

    public CannedResponse(
        Guid id,
        string title,
        string content,
        Guid? categoryId = null,
        bool isPublic = true)
        : base(id)
    {
        SetTitle(title);
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), CannedResponseConsts.MaxContentLength);
        CategoryId = categoryId;
        IsPublic = isPublic;
    }

    public CannedResponse SetTitle(string title)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), CannedResponseConsts.MaxTitleLength);
        return this;
    }
}
