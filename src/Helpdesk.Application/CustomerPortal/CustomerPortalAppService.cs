using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.CustomerPortal.Dtos;
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
    private readonly Volo.Abp.BlobStoring.IBlobContainer _blobContainer;
    private readonly TicketManager _ticketManager;
    private readonly SlaManager _slaManager;

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
        Volo.Abp.BlobStoring.IBlobContainer blobContainer,
        TicketManager ticketManager,
        SlaManager slaManager)
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
        _blobContainer = blobContainer;
        _ticketManager = ticketManager;
        _slaManager = slaManager;
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

        var dtos = pagedItems.Select(item => new CustomerTicketDto
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
            CommentCount = commentCounts.GetValueOrDefault(item.Ticket.Id, 0)
        }).ToList();

        return new PagedResultDto<CustomerTicketDto>(totalCount, dtos);
    }

    public async Task<CustomerTicketDetailDto> GetMyTicketAsync(Guid id)
    {
        var ticket = await _ticketRepository.GetAsync(id);
        CheckCustomerAccess(ticket);

        var category = await _categoryRepository.FindAsync(ticket.CategoryId);
        var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
        var status = await _statusRepository.FindAsync(ticket.StatusId);

        // Fetch ONLY non-internal comments
        var commentQuery = await _commentRepository.GetQueryableAsync();
        var comments = await AsyncExecuter.ToListAsync(
            commentQuery
                .Where(c => c.TicketId == id && !c.IsInternal)
                .OrderBy(c => c.CreationTime)
        );

        // Fetch attachments for ticket and public comments
        var attachmentQuery = await _attachmentRepository.GetQueryableAsync();
        var attachments = await AsyncExecuter.ToListAsync(
            attachmentQuery
                .Where(a => a.TicketId == id)
                .OrderBy(a => a.CreationTime)
        );

        var creatorIds = comments.Where(c => c.CreatorId.HasValue).Select(c => c.CreatorId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => creatorIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        var currentUserId = CurrentUser.GetId();

        var commentDtos = comments.Select(c => new CustomerCommentDto
        {
            Id = c.Id,
            Content = c.Content,
            CreationTime = c.CreationTime,
            CreatorId = c.CreatorId,
            CreatorName = c.CreatorId == currentUserId
                ? "Tôi"
                : (c.CreatorId.HasValue && userDict.ContainsKey(c.CreatorId.Value) ? userDict[c.CreatorId.Value] : "Hỗ trợ viên"),
            IsFromSupport = c.CreatorId != currentUserId,
            Attachments = attachments.Where(a => a.CommentId == c.Id).Select(MapAttachmentDto).ToList()
        }).ToList();

        var ticketAttachments = attachments.Where(a => a.CommentId == null).Select(MapAttachmentDto).ToList();

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
            requesterPhone: CurrentUser.PhoneNumber
        );

        await _slaManager.CalculateSlaDatesAsync(ticket);
        await _ticketRepository.InsertAsync(ticket, autoSave: true);

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
}
