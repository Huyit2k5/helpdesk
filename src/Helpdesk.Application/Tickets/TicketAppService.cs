using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.AssignmentRules;
using Helpdesk.CannedResponses;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Discord;
using Helpdesk.Notifications;
using Helpdesk.Permissions;
using Helpdesk.Priorities;
using Helpdesk.Tickets.Dtos;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Helpdesk.Sla;
using Microsoft.AspNetCore.Authorization;
using MiniExcelLibs;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Linq;

namespace Helpdesk.Tickets;

[Authorize(HelpdeskPermissions.Tickets.Default)]
public class TicketAppService : ApplicationService, ITicketAppService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketComment, Guid> _commentRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IRepository<TicketAttachment, Guid> _attachmentRepository;
    private readonly IBlobContainer _blobContainer;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<TicketSource, Guid> _sourceRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<SlaPolicy, Guid> _slaPolicyRepository;
    private readonly TicketManager _ticketManager;
    private readonly SlaManager _slaManager;
    private readonly AutoAssignmentManager _autoAssignmentManager;
    private readonly NotificationManager _notificationManager;
    private readonly IEmailSender _emailSender;
    private readonly IDiscordNotificationService _discordNotificationService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IDiscordBotService _discordBotService;

    public TicketAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketComment, Guid> commentRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IRepository<TicketAttachment, Guid> attachmentRepository,
        IBlobContainer blobContainer,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<TicketSource, Guid> sourceRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<SlaPolicy, Guid> slaPolicyRepository,
        TicketManager ticketManager,
        SlaManager slaManager,
        AutoAssignmentManager autoAssignmentManager,
        NotificationManager notificationManager,
        IEmailSender emailSender,
        IDiscordNotificationService discordNotificationService,
        IEmailNotificationService emailNotificationService,
        IDiscordBotService discordBotService)
    {
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _activityRepository = activityRepository;
        _attachmentRepository = attachmentRepository;
        _blobContainer = blobContainer;
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _sourceRepository = sourceRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _slaPolicyRepository = slaPolicyRepository;
        _ticketManager = ticketManager;
        _slaManager = slaManager;
        _autoAssignmentManager = autoAssignmentManager;
        _notificationManager = notificationManager;
        _emailSender = emailSender;
        _discordNotificationService = discordNotificationService;
        _emailNotificationService = emailNotificationService;
        _discordBotService = discordBotService;
    }

    public async Task<PagedResultDto<TicketListDto>> GetListAsync(GetTicketListInput input)
    {
        var ticketQuery = await _ticketRepository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();
        var priorityQuery = await _priorityRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();
        var sourceQuery = await _sourceRepository.GetQueryableAsync();
        var departmentQuery = await _departmentRepository.GetQueryableAsync();
        var userQuery = await _userRepository.GetQueryableAsync();
        var commentQuery = await _commentRepository.GetQueryableAsync();

        // Áp dụng bộ lọc
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            ticketQuery = ticketQuery.Where(t =>
                t.TicketNumber.ToLower().Contains(f) ||
                t.Title.ToLower().Contains(f) ||
                t.RequesterName.ToLower().Contains(f) ||
                t.RequesterEmail.ToLower().Contains(f) ||
                (t.Tags != null && t.Tags.ToLower().Contains(f)));
        }

        if (input.StatusId.HasValue && input.StatusId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.StatusId == input.StatusId.Value);
        }

        if (input.PriorityId.HasValue && input.PriorityId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.PriorityId == input.PriorityId.Value);
        }

        if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.CategoryId == input.CategoryId.Value);
        }

        if (input.DepartmentId.HasValue && input.DepartmentId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }

        if (input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.AssigneeId == input.AssigneeId.Value);
        }

        if (input.SourceId.HasValue && input.SourceId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.SourceId == input.SourceId.Value);
        }

        if (input.DateFrom.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime >= input.DateFrom.Value);
        }

        if (input.DateTo.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime <= input.DateTo.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(ticketQuery);

        // Sort & Paging
        var sorting = !string.IsNullOrWhiteSpace(input.Sorting) ? input.Sorting : "CreationTime DESC";
        ticketQuery = ticketQuery.OrderBy(sorting).Skip(input.SkipCount).Take(input.MaxResultCount);

        var rawList = await AsyncExecuter.ToListAsync(ticketQuery);
        var ticketIds = rawList.Select(t => t.Id).ToList();

        // Batch lookup
        var categoryIds = rawList.Select(t => t.CategoryId).Distinct().ToList();
        var catList = await AsyncExecuter.ToListAsync(categoryQuery.Where(c => categoryIds.Contains(c.Id)));
        var categories = catList.ToDictionary(c => c.Id, c => c.Name);

        var priorityIds = rawList.Select(t => t.PriorityId).Distinct().ToList();
        var priList = await AsyncExecuter.ToListAsync(priorityQuery.Where(p => priorityIds.Contains(p.Id)));
        var priorities = priList.ToDictionary(p => p.Id, p => new { p.Name, p.Color });

        var statusIds = rawList.Select(t => t.StatusId).Distinct().ToList();
        var statList = await AsyncExecuter.ToListAsync(statusQuery.Where(s => statusIds.Contains(s.Id)));
        var statuses = statList.ToDictionary(s => s.Id, s => new { s.Name, s.Color, StatusGroup = (int)s.StatusGroup });

        var sourceIds = rawList.Select(t => t.SourceId).Distinct().ToList();
        var srcList = await AsyncExecuter.ToListAsync(sourceQuery.Where(s => sourceIds.Contains(s.Id)));
        var sources = srcList.ToDictionary(s => s.Id, s => s.Name);

        var deptIds = rawList.Where(t => t.DepartmentId.HasValue).Select(t => t.DepartmentId!.Value).Distinct().ToList();
        var deptList = await AsyncExecuter.ToListAsync(departmentQuery.Where(d => deptIds.Contains(d.Id)));
        var departments = deptList.ToDictionary(d => d.Id, d => d.Name);

        var assigneeIds = rawList.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value).Distinct().ToList();
        var userList = await AsyncExecuter.ToListAsync(userQuery.Where(u => assigneeIds.Contains(u.Id)));
        var assignees = userList.ToDictionary(u => u.Id, u => u.UserName);

        var slaPolicyQuery = await _slaPolicyRepository.GetQueryableAsync();
        var slaPolicyIds = rawList.Where(t => t.SlaPolicyId.HasValue).Select(t => t.SlaPolicyId!.Value).Distinct().ToList();
        var slaList = await AsyncExecuter.ToListAsync(slaPolicyQuery.Where(s => slaPolicyIds.Contains(s.Id)));
        var slaPolicies = slaList.ToDictionary(s => s.Id, s => s.Name);

        var commentItems = await AsyncExecuter.ToListAsync(commentQuery.Where(c => ticketIds.Contains(c.TicketId)));
        var commentCounts = commentItems
            .GroupBy(c => c.TicketId)
            .ToDictionary(g => g.Key, g => g.Count());

        var dtos = rawList.Select(t =>
        {
            var pInfo = priorities.GetValueOrDefault(t.PriorityId);
            var sInfo = statuses.GetValueOrDefault(t.StatusId);

            return new TicketListDto
            {
                Id = t.Id,
                CreationTime = t.CreationTime,
                CreatorId = t.CreatorId,
                LastModificationTime = t.LastModificationTime,
                LastModifierId = t.LastModifierId,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                CategoryId = t.CategoryId,
                CategoryName = categories.GetValueOrDefault(t.CategoryId, string.Empty),
                PriorityId = t.PriorityId,
                PriorityName = pInfo?.Name ?? string.Empty,
                PriorityColor = pInfo?.Color,
                StatusId = t.StatusId,
                StatusName = sInfo?.Name ?? string.Empty,
                StatusColor = sInfo?.Color,
                StatusGroup = sInfo?.StatusGroup ?? 0,
                SourceId = t.SourceId,
                SourceName = sources.GetValueOrDefault(t.SourceId, string.Empty),
                DepartmentId = t.DepartmentId,
                DepartmentName = t.DepartmentId.HasValue ? departments.GetValueOrDefault(t.DepartmentId.Value) : null,
                AssigneeId = t.AssigneeId,
                AssigneeName = t.AssigneeId.HasValue ? assignees.GetValueOrDefault(t.AssigneeId.Value) : null,
                RequesterName = t.RequesterName,
                RequesterEmail = t.RequesterEmail,
                DueDate = t.DueDate,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt,
                SlaPolicyId = t.SlaPolicyId,
                SlaPolicyName = t.SlaPolicyId.HasValue ? slaPolicies.GetValueOrDefault(t.SlaPolicyId.Value) : null,
                FirstResponseDueDate = t.FirstResponseDueDate,
                FirstRespondedAt = t.FirstRespondedAt,
                IsFirstResponseBreached = t.IsFirstResponseBreached,
                IsResolutionBreached = t.IsResolutionBreached,
                Tags = t.Tags,
                CommentCount = commentCounts.GetValueOrDefault(t.Id, 0)
            };
        }).ToList();

        return new PagedResultDto<TicketListDto>(totalCount, dtos);
    }

    public async Task<TicketDetailDto> GetAsync(Guid id)
    {
        var ticket = await _ticketRepository.GetAsync(id);

        var category = await _categoryRepository.FindAsync(ticket.CategoryId);
        var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
        var status = await _statusRepository.FindAsync(ticket.StatusId);
        var source = await _sourceRepository.FindAsync(ticket.SourceId);
        var department = ticket.DepartmentId.HasValue ? await _departmentRepository.FindAsync(ticket.DepartmentId.Value) : null;
        var assignee = ticket.AssigneeId.HasValue ? await _userRepository.FindAsync(ticket.AssigneeId.Value) : null;
        var slaPolicy = ticket.SlaPolicyId.HasValue ? await _slaPolicyRepository.FindAsync(ticket.SlaPolicyId.Value) : null;

        var comments = await _commentRepository.GetListAsync(c => c.TicketId == id);
        var activities = await _activityRepository.GetListAsync(a => a.TicketId == id);
        var attachments = await _attachmentRepository.GetListAsync(a => a.TicketId == id);

        var commentCreatorIds = comments.Where(c => c.CreatorId.HasValue).Select(c => c.CreatorId!.Value).Distinct().ToList();
        var activityCreatorIds = activities.Where(a => a.CreatorId.HasValue).Select(a => a.CreatorId!.Value).Distinct().ToList();
        var attachmentCreatorIds = attachments.Where(a => a.CreatorId.HasValue).Select(a => a.CreatorId!.Value).Distinct().ToList();
        var allUserIds = commentCreatorIds.Union(activityCreatorIds).Union(attachmentCreatorIds).Distinct().ToList();

        var users = await _userRepository.GetListAsync(u => allUserIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        return new TicketDetailDto
        {
            Id = ticket.Id,
            CreationTime = ticket.CreationTime,
            CreatorId = ticket.CreatorId,
            LastModificationTime = ticket.LastModificationTime,
            LastModifierId = ticket.LastModifierId,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            PriorityId = ticket.PriorityId,
            PriorityName = priority?.Name ?? string.Empty,
            PriorityColor = priority?.Color,
            StatusId = ticket.StatusId,
            StatusName = status?.Name ?? string.Empty,
            StatusColor = status?.Color,
            StatusGroup = status != null ? (int)status.StatusGroup : 0,
            IsFinalStatus = status?.IsFinal ?? false,
            SourceId = ticket.SourceId,
            SourceName = source?.Name ?? string.Empty,
            DepartmentId = ticket.DepartmentId,
            DepartmentName = department?.Name,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = assignee?.UserName,
            RequesterId = ticket.RequesterId,
            RequesterName = ticket.RequesterName,
            RequesterEmail = ticket.RequesterEmail,
            RequesterPhone = ticket.RequesterPhone,
            DueDate = ticket.DueDate,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            SlaPolicyId = ticket.SlaPolicyId,
            SlaPolicyName = slaPolicy?.Name,
            FirstResponseDueDate = ticket.FirstResponseDueDate,
            FirstRespondedAt = ticket.FirstRespondedAt,
            IsFirstResponseBreached = ticket.IsFirstResponseBreached,
            IsResolutionBreached = ticket.IsResolutionBreached,
            Tags = ticket.Tags,
            CsatRating = ticket.CsatRating,
            CsatComment = ticket.CsatComment,
            CsatSubmittedAt = ticket.CsatSubmittedAt,
            Comments = comments.OrderBy(c => c.CreationTime).Select(c => new TicketCommentDto
            {
                Id = c.Id,
                CreationTime = c.CreationTime,
                CreatorId = c.CreatorId,
                TicketId = c.TicketId,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatorName = c.CreatorId.HasValue ? userDict.GetValueOrDefault(c.CreatorId.Value) : null,
                Attachments = attachments.Where(a => a.CommentId == c.Id).OrderBy(a => a.CreationTime).Select(a => new TicketAttachmentDto
                {
                    Id = a.Id,
                    TicketId = a.TicketId,
                    CommentId = a.CommentId,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    CreationTime = a.CreationTime,
                    CreatorId = a.CreatorId,
                    CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null
                }).ToList()
            }).ToList(),
            Activities = activities.OrderByDescending(a => a.CreationTime).Select(a => new TicketActivityDto
            {
                Id = a.Id,
                CreationTime = a.CreationTime,
                CreatorId = a.CreatorId,
                TicketId = a.TicketId,
                ActivityType = a.ActivityType,
                FieldName = a.FieldName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                Description = a.Description,
                CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null
            }).ToList(),
            Attachments = attachments.OrderByDescending(a => a.CreationTime).Select(a => new TicketAttachmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                CommentId = a.CommentId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                CreationTime = a.CreationTime,
                CreatorId = a.CreatorId,
                CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null
            }).ToList()
        };
    }

    [Authorize(HelpdeskPermissions.Tickets.Create)]
    public async Task<TicketDetailDto> CreateAsync(CreateTicketDto input)
    {
        var ticket = await _ticketManager.CreateAsync(
            input.Title,
            input.Description,
            input.CategoryId,
            input.PriorityId,
            input.StatusId,
            input.SourceId,
            input.RequesterName,
            input.RequesterEmail,
            input.DepartmentId,
            input.AssigneeId,
            input.RequesterId,
            input.RequesterPhone,
            input.DueDate,
            input.Tags
        );

        await _slaManager.CalculateSlaDatesAsync(ticket);

        if (!ticket.AssigneeId.HasValue || ticket.AssigneeId.Value == Guid.Empty)
        {
            await _autoAssignmentManager.TryAssignTicketAsync(ticket);
        }

        await _ticketRepository.InsertAsync(ticket, autoSave: true);

        var category = await _categoryRepository.FindAsync(ticket.CategoryId);
        var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
        var isCritical = priority?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) == true;
        await _discordNotificationService.SendTicketCreatedAsync(ticket, category?.Name ?? "Chung", priority?.Name ?? "Bình thường", isCritical);

        // Gửi email xác nhận tiếp nhận sự vụ cho khách hàng
        if (!string.IsNullOrWhiteSpace(ticket.RequesterEmail))
        {
            await _emailNotificationService.SendTicketCreatedConfirmationAsync(
                ticket.RequesterEmail,
                ticket.RequesterName,
                ticket.TicketNumber,
                ticket.Title,
                ticket.Description,
                ticket.DueDate
            );
        }

        // Nếu đã được phân công ngay lúc tạo (qua auto-assignment), gửi email cho kỹ thuật viên
        if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != Guid.Empty)
        {
            var assignee = await _userRepository.FindAsync(ticket.AssigneeId.Value);
            if (assignee != null && !string.IsNullOrWhiteSpace(assignee.Email))
            {
                await _emailNotificationService.SendTicketAssignedNotificationAsync(
                    assignee.Email,
                    assignee.UserName,
                    ticket.TicketNumber,
                    ticket.Title,
                    priority?.Name ?? "Bình thường",
                    ticket.DueDate
                );
            }
        }

        return await GetAsync(ticket.Id);
    }

    [Authorize(HelpdeskPermissions.Tickets.Assign)]
    public async Task<TicketDetailDto> AutoAssignAsync(Guid id)
    {
        var ticket = await _ticketRepository.GetAsync(id);
        var assigned = await _autoAssignmentManager.TryAssignTicketAsync(ticket);
        if (assigned)
        {
            await _ticketRepository.UpdateAsync(ticket, autoSave: true);
        }
        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.Edit)]
    public async Task<TicketDetailDto> UpdateAsync(Guid id, UpdateTicketDto input)
    {
        var ticket = await _ticketRepository.GetAsync(id);

        ticket.SetTitle(input.Title);
        ticket.Description = input.Description ?? string.Empty;
        ticket.CategoryId = input.CategoryId;
        ticket.PriorityId = input.PriorityId;
        ticket.StatusId = input.StatusId;
        ticket.SourceId = input.SourceId;
        ticket.DepartmentId = input.DepartmentId;
        ticket.AssigneeId = input.AssigneeId;
        ticket.RequesterName = input.RequesterName;
        ticket.RequesterEmail = input.RequesterEmail;
        ticket.RequesterPhone = input.RequesterPhone;
        ticket.DueDate = input.DueDate;
        ticket.Tags = input.Tags;

        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _ticketRepository.DeleteAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.Assign)]
    public async Task<TicketDetailDto> AssignAsync(Guid id, AssignTicketInput input)
    {
        var ticket = await _ticketRepository.GetAsync(id);

        string? assigneeName = null;
        if (input.AssigneeId.HasValue)
        {
            var user = await _userRepository.FindAsync(input.AssigneeId.Value);
            assigneeName = user?.UserName;
        }

        string? departmentName = null;
        if (input.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.FindAsync(input.DepartmentId.Value);
            departmentName = dept?.Name;
        }

        await _ticketManager.AssignAsync(ticket, input.AssigneeId, assigneeName, input.DepartmentId, departmentName);
        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        if (input.AssigneeId.HasValue)
        {
            await _notificationManager.CreateAsync(
                input.AssigneeId.Value,
                NotificationType.TicketAssigned,
                "Bạn được giao vé mới",
                $"Vé {ticket.TicketNumber} \"{ticket.Title}\" vừa được phân công cho bạn.",
                ticket.Id
            );

            var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
            await _discordNotificationService.SendTicketAssignedAsync(ticket, assigneeName ?? "Kỹ thuật viên", priority?.Name ?? "Normal");

            var assigneeUser = await _userRepository.FindAsync(input.AssigneeId.Value);
            if (!string.IsNullOrWhiteSpace(assigneeUser?.Email))
            {
                await _emailNotificationService.SendTicketAssignedNotificationAsync(
                    assigneeUser.Email,
                    assigneeName ?? assigneeUser.UserName,
                    ticket.TicketNumber,
                    ticket.Title,
                    priority?.Name ?? "Bình thường",
                    ticket.DueDate
                );
            }
        }

        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.ChangeStatus)]
    public async Task<TicketDetailDto> ChangeStatusAsync(Guid id, ChangeTicketStatusInput input)
    {
        var ticket = await _ticketRepository.GetAsync(id);
        var oldStatus = await _statusRepository.GetAsync(ticket.StatusId);
        var newStatus = await _statusRepository.GetAsync(input.StatusId);

        await _ticketManager.ChangeStatusAsync(ticket, oldStatus, newStatus);

        var isResolved = newStatus.IsFinal || newStatus.StatusGroup == StatusGroup.Closed;
        if (isResolved)
        {
            await _slaManager.OnTicketResolvedAsync(ticket);
        }

        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        if (!string.IsNullOrWhiteSpace(input.Comment))
        {
            var comment = new TicketComment(
                GuidGenerator.Create(),
                ticket.Id,
                input.Comment,
                isInternal: false
            );
            await _commentRepository.InsertAsync(comment);
        }

        if (ticket.RequesterId.HasValue)
        {
            await _notificationManager.CreateAsync(
                ticket.RequesterId.Value,
                NotificationType.StatusChanged,
                "Trạng thái vé đã thay đổi",
                $"Vé {ticket.TicketNumber} \"{ticket.Title}\" đã chuyển sang trạng thái \"{newStatus.Name}\".",
                ticket.Id
            );
        }

        if (isResolved && !string.IsNullOrWhiteSpace(ticket.RequesterEmail))
        {
            await _emailNotificationService.SendTicketResolvedNotificationAsync(
                ticket.RequesterEmail,
                ticket.RequesterName,
                ticket.TicketNumber,
                ticket.Title,
                input.Comment
            );
        }

        if (isResolved)
        {
            var resolverName = CurrentUser.UserName ?? "Kỹ thuật viên";
            await _discordNotificationService.SendTicketResolvedAsync(ticket, resolverName);
        }

        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.AddComment)]
    public async Task<TicketCommentDto> AddCommentAsync(Guid id, CreateTicketCommentDto input)
    {
        var ticket = await _ticketRepository.GetAsync(id);

        var comment = new TicketComment(
            GuidGenerator.Create(),
            ticket.Id,
            input.Content,
            input.IsInternal
        );

        await _commentRepository.InsertAsync(comment);

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            input.IsInternal ? TicketActivityType.InternalNoteAdded : TicketActivityType.CommentAdded,
            description: input.IsInternal ? "Đã thêm một ghi chú nội bộ." : "Đã phản hồi vé hỗ trợ."
        );

        await _activityRepository.InsertAsync(activity);

        await _slaManager.OnCommentAddedAsync(ticket, input.IsInternal);
        await _ticketRepository.UpdateAsync(ticket);

        if (!input.IsInternal && ticket.RequesterId.HasValue)
        {
            await _notificationManager.CreateAsync(
                ticket.RequesterId.Value,
                NotificationType.CommentAdded,
                "Có phản hồi mới trên vé của bạn",
                $"Vé {ticket.TicketNumber} \"{ticket.Title}\" vừa nhận được phản hồi mới từ đội hỗ trợ.",
                ticket.Id
            );
        }

        // Đồng bộ bình luận công khai ra Discord Thread nếu sự vụ có luồng thảo luận Discord
        if (!input.IsInternal && !string.IsNullOrWhiteSpace(ticket.DiscordThreadId) && ulong.TryParse(ticket.DiscordThreadId, out var threadId))
        {
            await _discordBotService.SendMessageToThreadAsync(threadId, CurrentUser.UserName ?? "Kỹ thuật viên", input.Content);
        }

        return new TicketCommentDto
        {
            Id = comment.Id,
            CreationTime = comment.CreationTime,
            CreatorId = comment.CreatorId,
            TicketId = comment.TicketId,
            Content = comment.Content,
            IsInternal = comment.IsInternal,
            CreatorName = CurrentUser.UserName
        };
    }

    public async Task<List<TicketCommentDto>> GetCommentsAsync(Guid id)
    {
        var comments = await _commentRepository.GetListAsync(c => c.TicketId == id);
        var userIds = comments.Where(c => c.CreatorId.HasValue).Select(c => c.CreatorId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        return comments.OrderBy(c => c.CreationTime).Select(c => new TicketCommentDto
        {
            Id = c.Id,
            CreationTime = c.CreationTime,
            CreatorId = c.CreatorId,
            TicketId = c.TicketId,
            Content = c.Content,
            IsInternal = c.IsInternal,
            CreatorName = c.CreatorId.HasValue ? userDict.GetValueOrDefault(c.CreatorId.Value) : (c.AuthorName ?? "Khách hàng")
        }).ToList();
    }

    public async Task<List<TicketActivityDto>> GetActivitiesAsync(Guid id)
    {
        var activities = await _activityRepository.GetListAsync(a => a.TicketId == id);
        var userIds = activities.Where(a => a.CreatorId.HasValue).Select(a => a.CreatorId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        return activities.OrderByDescending(a => a.CreationTime).Select(a => new TicketActivityDto
        {
            Id = a.Id,
            CreationTime = a.CreationTime,
            CreatorId = a.CreatorId,
            TicketId = a.TicketId,
            ActivityType = a.ActivityType,
            FieldName = a.FieldName,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            Description = a.Description,
            CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null
        }).ToList();
    }

    [Authorize(HelpdeskPermissions.Tickets.Default)]
    public async Task<TicketAttachmentDto> UploadAttachmentAsync(Guid id, IRemoteStreamContent file, Guid? commentId = null)
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

        var ticket = await _ticketRepository.GetAsync(id);
        var ext = Path.GetExtension(file.FileName);
        var blobName = $"{ticket.Id}/{Guid.NewGuid():N}{ext}";

        using var memoryStream = new MemoryStream();
        await file.GetStream().CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        await _blobContainer.SaveAsync(blobName, memoryStream.ToArray(), overrideExisting: true);

        var attachment = new TicketAttachment(
            GuidGenerator.Create(),
            ticket.Id,
            file.FileName,
            file.ContentLength ?? memoryStream.Length,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            blobName,
            commentId);

        await _attachmentRepository.InsertAsync(attachment);

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.AttachmentAdded,
            "Attachment",
            null,
            file.FileName,
            $"Đã tải lên tệp đính kèm: {file.FileName}");
        await _activityRepository.InsertAsync(activity);

        return new TicketAttachmentDto
        {
            Id = attachment.Id,
            TicketId = attachment.TicketId,
            CommentId = attachment.CommentId,
            FileName = attachment.FileName,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            CreationTime = attachment.CreationTime,
            CreatorId = CurrentUser.Id,
            CreatorName = CurrentUser.UserName
        };
    }

    public async Task<List<TicketAttachmentDto>> GetAttachmentsAsync(Guid id)
    {
        var attachments = await _attachmentRepository.GetListAsync(a => a.TicketId == id);
        var userIds = attachments.Where(a => a.CreatorId.HasValue).Select(a => a.CreatorId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => userIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        return attachments.OrderByDescending(a => a.CreationTime).Select(a => new TicketAttachmentDto
        {
            Id = a.Id,
            TicketId = a.TicketId,
            CommentId = a.CommentId,
            FileName = a.FileName,
            FileSize = a.FileSize,
            ContentType = a.ContentType,
            CreationTime = a.CreationTime,
            CreatorId = a.CreatorId,
            CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null
        }).ToList();
    }

    public async Task<IRemoteStreamContent> DownloadAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetAsync(attachmentId);
        var stream = await _blobContainer.GetAsync(attachment.BlobName);
        return new RemoteStreamContent(stream, attachment.FileName, attachment.ContentType);
    }

    [Authorize(HelpdeskPermissions.Tickets.Default)]
    public async Task DeleteAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetAsync(attachmentId);
        await _blobContainer.DeleteAsync(attachment.BlobName);
        await _attachmentRepository.DeleteAsync(attachment);

        var activity = new TicketActivity(
            GuidGenerator.Create(),
            attachment.TicketId,
            TicketActivityType.AttachmentAdded,
            "Attachment",
            attachment.FileName,
            null,
            $"Đã xóa tệp đính kèm: {attachment.FileName}");
        await _activityRepository.InsertAsync(activity);
    }

    public async Task<IRemoteStreamContent> ExportExcelAsync(GetTicketListInput input)
    {
        var ticketQuery = await _ticketRepository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();
        var priorityQuery = await _priorityRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();
        var departmentQuery = await _departmentRepository.GetQueryableAsync();
        var userQuery = await _userRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            ticketQuery = ticketQuery.Where(t =>
                t.TicketNumber.ToLower().Contains(f) ||
                t.Title.ToLower().Contains(f) ||
                t.RequesterName.ToLower().Contains(f) ||
                t.RequesterEmail.ToLower().Contains(f) ||
                (t.Tags != null && t.Tags.ToLower().Contains(f)));
        }

        if (input.StatusId.HasValue && input.StatusId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.StatusId == input.StatusId.Value);
        }

        if (input.PriorityId.HasValue && input.PriorityId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.PriorityId == input.PriorityId.Value);
        }

        if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.CategoryId == input.CategoryId.Value);
        }

        if (input.DepartmentId.HasValue && input.DepartmentId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }

        if (input.AssigneeId.HasValue && input.AssigneeId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.AssigneeId == input.AssigneeId.Value);
        }

        if (input.SourceId.HasValue && input.SourceId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.SourceId == input.SourceId.Value);
        }

        if (input.DateFrom.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime >= input.DateFrom.Value);
        }

        if (input.DateTo.HasValue)
        {
            var toDate = input.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            ticketQuery = ticketQuery.Where(t => t.CreationTime <= toDate);
        }

        var rawList = await AsyncExecuter.ToListAsync(ticketQuery.OrderByDescending(t => t.CreationTime));

        var categories = (await AsyncExecuter.ToListAsync(categoryQuery)).ToDictionary(c => c.Id, c => c.Name);
        var priorities = (await AsyncExecuter.ToListAsync(priorityQuery)).ToDictionary(p => p.Id, p => p.Name);
        var statuses = (await AsyncExecuter.ToListAsync(statusQuery)).ToDictionary(s => s.Id, s => s.Name);
        var departments = (await AsyncExecuter.ToListAsync(departmentQuery)).ToDictionary(d => d.Id, d => d.Name);
        var users = (await AsyncExecuter.ToListAsync(userQuery)).ToDictionary(u => u.Id, u => u.UserName);

        var exportData = rawList.Select((t, index) => new Dictionary<string, object?>
        {
            ["STT"] = index + 1,
            ["Mã Sự Vụ"] = t.TicketNumber,
            ["Tiêu Đề"] = t.Title,
            ["Người Yêu Cầu"] = t.RequesterName,
            ["Email"] = t.RequesterEmail,
            ["Danh Mục"] = categories.GetValueOrDefault(t.CategoryId, string.Empty),
            ["Độ Ưu Tiên"] = priorities.GetValueOrDefault(t.PriorityId, string.Empty),
            ["Trạng Thái"] = statuses.GetValueOrDefault(t.StatusId, string.Empty),
            ["Phòng Ban"] = t.DepartmentId.HasValue ? departments.GetValueOrDefault(t.DepartmentId.Value) : "",
            ["Người Xử Lý"] = t.AssigneeId.HasValue ? users.GetValueOrDefault(t.AssigneeId.Value) : "Chưa phân công",
            ["Ngày Tạo"] = t.CreationTime.ToString("dd/MM/yyyy HH:mm"),
            ["Hạn Xử Lý (SLA)"] = t.DueDate.HasValue ? t.DueDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
            ["Ngày Giải Quyết"] = t.ResolvedAt.HasValue ? t.ResolvedAt.Value.ToString("dd/MM/yyyy HH:mm") : "",
            ["Tình Trạng SLA"] = t.IsResolutionBreached ? "Vi phạm" : (t.ResolvedAt.HasValue ? "Đạt SLA" : "Trong hạn")
        }).ToList();

        var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(exportData);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var fileName = $"SuVu_Helpdesk_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
