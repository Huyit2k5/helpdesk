using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Assets;
using Helpdesk.Assets.Dtos;
using Helpdesk.AssignmentRules;
using Helpdesk.Categories;
using Helpdesk.CustomerPortal.Dtos;
using Helpdesk.Discord;
using Helpdesk.Notifications;
using Helpdesk.Permissions;
using Helpdesk.Priorities;
using Helpdesk.Sla;
using Helpdesk.Tickets;
using Helpdesk.Tickets.Dtos;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace Helpdesk.CustomerPortal;

[Authorize]
public class CustomerPortalAppService : ApplicationService, ICustomerPortalAppService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketComment, Guid> _commentRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IRepository<TicketAttachment, Guid> _attachmentRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<TicketSource, Guid> _sourceRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Asset, Guid> _assetRepository;
    private readonly AssetManager _assetManager;
    private readonly Volo.Abp.BlobStoring.IBlobContainer _blobContainer;
    private readonly TicketManager _ticketManager;
    private readonly SlaManager _slaManager;
    private readonly AutoAssignmentManager _autoAssignmentManager;
    private readonly NotificationManager _notificationManager;
    private readonly IDiscordNotificationService _discordNotificationService;

    public CustomerPortalAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketComment, Guid> commentRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IRepository<TicketAttachment, Guid> attachmentRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<TicketSource, Guid> sourceRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<Asset, Guid> assetRepository,
        AssetManager assetManager,
        Volo.Abp.BlobStoring.IBlobContainer blobContainer,
        TicketManager ticketManager,
        SlaManager slaManager,
        AutoAssignmentManager autoAssignmentManager,
        NotificationManager notificationManager,
        IDiscordNotificationService discordNotificationService)
    {
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _activityRepository = activityRepository;
        _attachmentRepository = attachmentRepository;
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _sourceRepository = sourceRepository;
        _userRepository = userRepository;
        _assetRepository = assetRepository;
        _assetManager = assetManager;
        _blobContainer = blobContainer;
        _ticketManager = ticketManager;
        _slaManager = slaManager;
        _autoAssignmentManager = autoAssignmentManager;
        _notificationManager = notificationManager;
        _discordNotificationService = discordNotificationService;
    }

    public async Task<PagedResultDto<CustomerTicketDto>> GetMyTicketsAsync(GetCustomerTicketListInput input)
    {
        var currentUserId = CurrentUser.GetId();
        var currentUserEmail = CurrentUser.Email;

        var ticketQuery = (await _ticketRepository.GetQueryableAsync())
            .Where(t => t.RequesterId == currentUserId || (currentUserEmail != null && t.RequesterEmail == currentUserEmail));

        var categoryQuery = await _categoryRepository.GetQueryableAsync();
        var priorityQuery = await _priorityRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();
        var commentQuery = await _commentRepository.GetQueryableAsync();

        if (input.CategoryId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CategoryId == input.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            ticketQuery = ticketQuery.Where(t => t.TicketNumber.ToLower().Contains(f) || t.Title.ToLower().Contains(f));
        }

        var baseQuery = from t in ticketQuery
                        join s in statusQuery on t.StatusId equals s.Id
                        join p in priorityQuery on t.PriorityId equals p.Id
                        join c in categoryQuery on t.CategoryId equals c.Id
                        select new
                        {
                            Ticket = t,
                            CategoryName = c.Name,
                            PriorityName = p.Name,
                            PriorityColor = p.Color,
                            StatusName = s.Name,
                            StatusColor = s.Color,
                            IsFinal = s.IsFinal
                        };

        if (input.IsClosed.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IsFinal == input.IsClosed.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(baseQuery);

        var pagedItems = await AsyncExecuter.ToListAsync(
            baseQuery
                .OrderByDescending(x => x.Ticket.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount)
        );

        var ticketIds = pagedItems.Select(x => x.Ticket.Id).ToList();

        // Count public comments only
        var comments = await AsyncExecuter.ToListAsync(
            commentQuery.Where(c => ticketIds.Contains(c.TicketId) && !c.IsInternal)
        );
        var commentCounts = comments
            .GroupBy(c => c.TicketId)
            .ToDictionary(g => g.Key, g => g.Count());

        var assetIds = pagedItems
            .Where(x => x.Ticket.AssetId.HasValue)
            .Select(x => x.Ticket.AssetId!.Value)
            .Distinct()
            .ToList();
        var assetDict = new Dictionary<Guid, Asset>();
        if (assetIds.Any())
        {
            var assetQuery = await _assetRepository.GetQueryableAsync();
            var assets = await AsyncExecuter.ToListAsync(assetQuery.Where(a => assetIds.Contains(a.Id)));
            assetDict = assets.ToDictionary(a => a.Id);
        }

        var dtos = pagedItems.Select(item =>
        {
            var asset = item.Ticket.AssetId.HasValue ? assetDict.GetValueOrDefault(item.Ticket.AssetId.Value) : null;
            return new CustomerTicketDto
            {
                Id = item.Ticket.Id,
                TicketNumber = item.Ticket.TicketNumber,
                Title = item.Ticket.Title,
                CategoryName = item.CategoryName,
                PriorityName = item.PriorityName,
                PriorityColor = item.PriorityColor ?? "#3b82f6",
                StatusName = item.StatusName,
                StatusColor = item.StatusColor ?? "#10b981",
                IsFinal = item.IsFinal,
                CreationTime = item.Ticket.CreationTime,
                LastModificationTime = item.Ticket.LastModificationTime,
                CommentCount = commentCounts.GetValueOrDefault(item.Ticket.Id, 0),
                CsatRating = item.Ticket.CsatRating,
                AssetId = item.Ticket.AssetId,
                AssetTag = asset?.AssetTag,
                AssetName = asset?.Name
            };
        }).ToList();

        return new PagedResultDto<CustomerTicketDto>(totalCount, dtos);
    }

    public async Task<CustomerTicketDetailDto> GetMyTicketAsync(Guid id)
    {
        var ticket = await _ticketRepository.GetAsync(id);
        CheckCustomerAccess(ticket);

        var category = ticket.CategoryId != Guid.Empty
            ? await _categoryRepository.FindAsync(ticket.CategoryId)
            : null;

        var priority = ticket.PriorityId != Guid.Empty
            ? await _priorityRepository.FindAsync(ticket.PriorityId)
            : null;

        var status = ticket.StatusId != Guid.Empty
            ? await _statusRepository.FindAsync(ticket.StatusId)
            : null;

        Asset? asset = null;
        if (ticket.AssetId.HasValue)
        {
            asset = await _assetRepository.FindAsync(ticket.AssetId.Value);
        }

        var attachmentQuery = await _attachmentRepository.GetQueryableAsync();
        var attachments = await AsyncExecuter.ToListAsync(
            attachmentQuery.Where(a => a.TicketId == id)
        );

        var ticketAttachments = attachments
            .Where(a => a.CommentId == null)
            .Select(MapAttachmentDto)
            .ToList();

        var commentAttachments = attachments
            .Where(a => a.CommentId != null)
            .GroupBy(a => a.CommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(MapAttachmentDto).ToList());

        var commentQuery = await _commentRepository.GetQueryableAsync();
        var comments = await AsyncExecuter.ToListAsync(
            commentQuery
                .Where(c => c.TicketId == id && !c.IsInternal)
                .OrderBy(c => c.CreationTime)
        );

        var userIds = comments
            .Where(c => c.CreatorId.HasValue)
            .Select(c => c.CreatorId!.Value)
            .Distinct()
            .ToList();

        var userQuery = await _userRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(
            userQuery.Where(u => userIds.Contains(u.Id))
        );
        var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

        var currentUserId = CurrentUser.GetId();
        var commentDtos = comments.Select(c => new CustomerCommentDto
        {
            Id = c.Id,
            TicketId = c.TicketId,
            Content = c.Content,
            CreationTime = c.CreationTime,
            CreatorId = c.CreatorId,
            CreatorName = c.CreatorId.HasValue ? userDict.GetValueOrDefault(c.CreatorId.Value, "Nhân viên hỗ trợ") : "Hệ thống",
            IsFromSupport = c.CreatorId != currentUserId,
            Attachments = commentAttachments.GetValueOrDefault(c.Id, new List<TicketAttachmentDto>())
        }).ToList();

        return new CustomerTicketDetailDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            PriorityId = ticket.PriorityId,
            PriorityName = priority?.Name ?? string.Empty,
            PriorityColor = priority?.Color ?? "#3b82f6",
            StatusId = ticket.StatusId,
            StatusName = status?.Name ?? string.Empty,
            StatusColor = status?.Color ?? "#10b981",
            IsFinal = status?.IsFinal ?? false,
            CreationTime = ticket.CreationTime,
            LastModificationTime = ticket.LastModificationTime,
            DueDate = ticket.DueDate,
            ResolvedAt = ticket.ResolvedAt,
            CsatRating = ticket.CsatRating,
            CsatComment = ticket.CsatComment,
            CsatSubmittedAt = ticket.CsatSubmittedAt,
            AssetId = ticket.AssetId,
            AssetTag = asset?.AssetTag,
            AssetName = asset?.Name,
            AssetTypeName = asset != null ? GetAssetTypeName(asset.AssetType) : null,
            SerialNumber = asset?.SerialNumber,
            Attachments = ticketAttachments,
            Comments = commentDtos
        };
    }

    public async Task<CustomerTicketDetailDto> CreateMyTicketAsync(CreateCustomerTicketDto input)
    {
        var currentUserId = CurrentUser.GetId();
        var currentUserName = CurrentUser.Name ?? CurrentUser.UserName ?? "Khách hàng";
        var currentUserEmail = CurrentUser.Email ?? "customer@company.com";

        // Determine default status
        var statusQuery = await _statusRepository.GetQueryableAsync();
        var defaultStatus = await AsyncExecuter.FirstOrDefaultAsync(statusQuery.Where(s => s.IsDefault))
            ?? await AsyncExecuter.FirstOrDefaultAsync(statusQuery);

        if (defaultStatus == null)
        {
            throw new BusinessException("Helpdesk:StatusNotConfigured", "Hệ thống chưa thiết lập trạng thái vé.");
        }

        // Determine priority
        Guid priorityId;
        if (input.PriorityId.HasValue && input.PriorityId.Value != Guid.Empty)
        {
            priorityId = input.PriorityId.Value;
        }
        else
        {
            var priorityQuery = await _priorityRepository.GetQueryableAsync();
            var defaultPriority = await AsyncExecuter.FirstOrDefaultAsync(priorityQuery.Where(p => p.Code == "MEDIUM" || p.Code == "NORMAL"))
                ?? await AsyncExecuter.FirstOrDefaultAsync(priorityQuery);

            if (defaultPriority == null)
            {
                throw new BusinessException("Helpdesk:PriorityNotConfigured", "Hệ thống chưa thiết lập độ ưu tiên.");
            }
            priorityId = defaultPriority.Id;
        }

        // Determine source (PORTAL or WEB)
        var sourceQuery = await _sourceRepository.GetQueryableAsync();
        var source = await AsyncExecuter.FirstOrDefaultAsync(sourceQuery.Where(s => s.Code == "PORTAL" || s.Code == "WEB_PORTAL" || s.Code == "WEB"))
            ?? await AsyncExecuter.FirstOrDefaultAsync(sourceQuery);

        if (source == null)
        {
            throw new BusinessException("Helpdesk:SourceNotConfigured", "Hệ thống chưa thiết lập nguồn tiếp nhận vé.");
        }

        var ticket = await _ticketManager.CreateAsync(
            input.Title,
            input.Description,
            input.CategoryId,
            priorityId,
            defaultStatus.Id,
            source.Id,
            currentUserName,
            currentUserEmail,
            departmentId: null,
            assigneeId: null,
            requesterId: currentUserId,
            requesterPhone: CurrentUser.PhoneNumber,
            assetId: input.AssetId
        );

        await _slaManager.CalculateSlaDatesAsync(ticket);
        await _autoAssignmentManager.TryAssignTicketAsync(ticket);
        await _ticketRepository.InsertAsync(ticket, autoSave: true);

        if (input.AssetId.HasValue)
        {
            var linkedAsset = await _assetRepository.FindAsync(input.AssetId.Value);
            if (linkedAsset != null)
            {
                await _activityRepository.InsertAsync(new TicketActivity(
                    GuidGenerator.Create(),
                    ticket.Id,
                    TicketActivityType.Created,
                    fieldName: "AssetId",
                    oldVal: null,
                    newVal: linkedAsset.AssetTag,
                    description: $"Khách hàng liên kết thiết bị [{linkedAsset.AssetTag}] {linkedAsset.Name} vào yêu cầu hỗ trợ"
                ));
            }
        }

        // Handle initial attachments
        if (input.Attachments != null && input.Attachments.Any())
        {
            foreach (var att in input.Attachments)
            {
                var attachment = new TicketAttachment(
                    GuidGenerator.Create(),
                    ticket.Id,
                    att.FileName,
                    att.FileSize,
                    att.ContentType,
                    att.BlobName
                );
                await _attachmentRepository.InsertAsync(attachment);
            }
        }

        var category = await _categoryRepository.FindAsync(ticket.CategoryId);
        var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
        var isCritical = priority?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) == true;
        await _discordNotificationService.SendTicketCreatedAsync(ticket, category?.Name ?? "Chung", priority?.Name ?? "Bình thường", isCritical);

        return await GetMyTicketAsync(ticket.Id);
    }

    public async Task<CustomerCommentDto> AddMyCommentAsync(Guid ticketId, AddCustomerCommentDto input)
    {
        var ticket = await _ticketRepository.GetAsync(ticketId);
        CheckCustomerAccess(ticket);

        var comment = new TicketComment(
            GuidGenerator.Create(),
            ticketId,
            input.Content,
            isInternal: false // Always public!
        );

        await _commentRepository.InsertAsync(comment, autoSave: true);

        var currentUserId = CurrentUser.GetId();
        var attachments = new List<TicketAttachmentDto>();

        if (input.Attachments != null && input.Attachments.Any())
        {
            foreach (var att in input.Attachments)
            {
                var attachment = new TicketAttachment(
                    GuidGenerator.Create(),
                    ticketId,
                    att.FileName,
                    att.FileSize,
                    att.ContentType,
                    att.BlobName,
                    commentId: comment.Id
                );
                await _attachmentRepository.InsertAsync(attachment, autoSave: true);
                attachments.Add(MapAttachmentDto(attachment));
            }
        }

        // Log public activity
        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticketId,
            TicketActivityType.CommentAdded,
            description: $"Khách hàng đã phản hồi: \"{(input.Content.Length > 50 ? input.Content[..50] + "..." : input.Content)}\""
        );
        await _activityRepository.InsertAsync(activity);

        if (ticket.AssigneeId.HasValue)
        {
            await _notificationManager.CreateAsync(
                ticket.AssigneeId.Value,
                NotificationType.CommentAdded,
                "Khách hàng vừa phản hồi",
                $"Vé {ticket.TicketNumber} \"{ticket.Title}\" vừa nhận được phản hồi mới từ khách hàng.",
                ticket.Id
            );
        }

        return new CustomerCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreationTime = comment.CreationTime,
            CreatorId = currentUserId,
            CreatorName = "Tôi",
            IsFromSupport = false,
            Attachments = attachments
        };
    }

    public async Task<TicketAttachmentDto> UploadMyAttachmentAsync(Guid ticketId, Volo.Abp.Content.IRemoteStreamContent file, Guid? commentId = null)
    {
        if (file == null || file.ContentLength == 0)
        {
            throw new UserFriendlyException("Tệp tải lên không hợp lệ hoặc trống.");
        }

        const long maxSizeBytes = 10 * 1024 * 1024; // 10MB
        if (file.ContentLength > maxSizeBytes)
        {
            throw new UserFriendlyException("Dung lượng tệp vượt quá giới hạn cho phép (tối đa 10 MB).");
        }

        var ticket = await _ticketRepository.GetAsync(ticketId);
        CheckCustomerAccess(ticket);

        var ext = System.IO.Path.GetExtension(file.FileName);
        var blobName = $"{ticket.Id}/{GuidGenerator.Create():N}{ext}";

        using var memoryStream = new System.IO.MemoryStream();
        await file.GetStream().CopyToAsync(memoryStream);
        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

        await _blobContainer.SaveAsync(blobName, memoryStream.ToArray(), overrideExisting: true);

        var attachment = new TicketAttachment(
            GuidGenerator.Create(),
            ticket.Id,
            file.FileName,
            file.ContentLength ?? memoryStream.Length,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            blobName,
            commentId);

        await _attachmentRepository.InsertAsync(attachment, autoSave: true);

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.AttachmentAdded,
            description: $"Khách hàng đã đính kèm tệp: {file.FileName}");
        await _activityRepository.InsertAsync(activity);

        return MapAttachmentDto(attachment);
    }

    public async Task<IRemoteStreamContent> DownloadMyAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetAsync(attachmentId);
        var ticket = await _ticketRepository.GetAsync(attachment.TicketId);
        CheckCustomerAccess(ticket);

        var stream = await _blobContainer.GetAsync(attachment.BlobName);
        return new RemoteStreamContent(stream, attachment.FileName, attachment.ContentType);
    }

    public async Task<CustomerTicketDetailDto> SubmitTicketFeedbackAsync(Guid ticketId, SubmitTicketFeedbackDto input)
    {
        var ticket = await _ticketRepository.GetAsync(ticketId);
        CheckCustomerAccess(ticket);

        var status = await _statusRepository.GetAsync(ticket.StatusId);
        var isResolvedOrClosed = ticket.ResolvedAt.HasValue || status.IsFinal;
        if (!isResolvedOrClosed)
        {
            throw new UserFriendlyException("Bạn chỉ có thể đánh giá dịch vụ khi yêu cầu hỗ trợ đã được giải quyết hoặc đóng.");
        }

        if (input.Rating < 1 || input.Rating > 5)
        {
            throw new UserFriendlyException("Điểm đánh giá phải từ 1 đến 5 sao.");
        }

        ticket.CsatRating = input.Rating;
        ticket.CsatComment = input.Comment?.Trim();
        ticket.CsatSubmittedAt = Clock.Now;

        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        var stars = new string('⭐', input.Rating);
        var activityDesc = $"Khách hàng đã đánh giá dịch vụ: {input.Rating}/5 sao {stars}";
        if (!string.IsNullOrWhiteSpace(input.Comment))
        {
            activityDesc += $". Nhận xét: \"{input.Comment}\"";
        }

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.CommentAdded,
            description: activityDesc);
        await _activityRepository.InsertAsync(activity);

        return await GetMyTicketAsync(ticket.Id);
    }

    private void CheckCustomerAccess(Ticket ticket)
    {
        var currentUserId = CurrentUser.GetId();
        var currentUserEmail = CurrentUser.Email;

        var isOwner = ticket.RequesterId == currentUserId ||
                      (!string.IsNullOrWhiteSpace(currentUserEmail) && ticket.RequesterEmail.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase));

        if (!isOwner)
        {
            throw new BusinessException("Helpdesk:AccessDenied", "Bạn không có quyền truy cập yêu cầu hỗ trợ này.");
        }
    }

    private static TicketAttachmentDto MapAttachmentDto(TicketAttachment a)
    {
        return new TicketAttachmentDto
        {
            Id = a.Id,
            TicketId = a.TicketId,
            CommentId = a.CommentId,
            FileName = a.FileName,
            FileSize = a.FileSize,
            ContentType = a.ContentType,
            CreationTime = a.CreationTime,
            CreatorId = a.CreatorId
        };
    }

    public async Task<List<AssetDto>> GetMyAssetsAsync()
    {
        var currentUserId = CurrentUser.GetId();
        var currentUserEmail = CurrentUser.Email;

        var queryable = await _assetRepository.GetQueryableAsync();

        queryable = queryable.Where(a =>
            (a.AssignedToUserId == currentUserId) ||
            (!string.IsNullOrEmpty(currentUserEmail) && a.AssignedToUserEmail == currentUserEmail));

        var items = await AsyncExecuter.ToListAsync(queryable.OrderBy(a => a.Name));
        return items.Select(MapToAssetDto).ToList();
    }

    private static AssetDto MapToAssetDto(Asset a)
    {
        return new AssetDto
        {
            Id = a.Id,
            AssetTag = a.AssetTag,
            Name = a.Name,
            AssetType = a.AssetType,
            AssetTypeName = GetAssetTypeName(a.AssetType),
            Status = a.Status,
            StatusName = GetStatusName(a.Status),
            SerialNumber = a.SerialNumber,
            Model = a.Model,
            Manufacturer = a.Manufacturer,
            Location = a.Location,
            PurchaseDate = a.PurchaseDate,
            WarrantyExpiryDate = a.WarrantyExpiryDate,
            PurchaseCost = a.PurchaseCost,
            AssignedToUserId = a.AssignedToUserId,
            AssignedToUserName = a.AssignedToUserName,
            AssignedToUserEmail = a.AssignedToUserEmail,
            Department = a.Department,
            AssignedDate = a.AssignedDate,
            Specifications = a.Specifications,
            Notes = a.Notes,
            IsHandoverConfirmed = a.IsHandoverConfirmed,
            HandoverConfirmedDate = a.HandoverConfirmedDate,
            HandoverNotes = a.HandoverNotes,
            CreationTime = a.CreationTime,
            CreatorId = a.CreatorId,
            LastModificationTime = a.LastModificationTime,
            LastModifierId = a.LastModifierId
        };
    }

    public async Task ConfirmAssetHandoverAsync(Guid assetId, ConfirmAssetHandoverDto input)
    {
        var asset = await _assetRepository.GetAsync(assetId);
        var currentUserId = CurrentUser.GetId();
        var currentUserEmail = CurrentUser.Email;

        if (asset.AssignedToUserId != currentUserId && (string.IsNullOrEmpty(currentUserEmail) || asset.AssignedToUserEmail != currentUserEmail))
        {
            throw new UserFriendlyException("Bạn không có quyền ký nhận cho thiết bị không thuộc tài khoản của bạn.");
        }

        await _assetManager.ConfirmHandoverAsync(
            asset,
            input?.Notes,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _assetRepository.UpdateAsync(asset);
    }

    private static string GetAssetTypeName(AssetType type)
    {
        return type switch
        {
            AssetType.Laptop => "Máy tính xách tay (Laptop)",
            AssetType.Desktop => "Máy tính để bàn (PC)",
            AssetType.Monitor => "Màn hình hiển thị",
            AssetType.NetworkDevice => "Thiết bị mạng (Router/Switch/AP)",
            AssetType.PrinterPeripheral => "Máy in & Ngoại vi",
            AssetType.ServerStorage => "Máy chủ & Lưu trữ",
            AssetType.SoftwareLicense => "Bản quyền phần mềm",
            AssetType.MobileDevice => "Thiết bị di động (Tablet/Phone)",
            _ => "Thiết bị khác"
        };
    }

    private static string GetStatusName(AssetStatus status)
    {
        return status switch
        {
            AssetStatus.InStock => "Trong kho lưu trữ",
            AssetStatus.Assigned => "Đang cấp phát",
            AssetStatus.UnderRepair => "Đang sửa chữa / Bảo hành",
            AssetStatus.Reserved => "Đã giữ chỗ / Đặt trước",
            AssetStatus.Retired => "Đã thanh lý",
            AssetStatus.LostStolen => "Thất lạc / Báo mất",
            _ => "Không xác định"
        };
    }
}
