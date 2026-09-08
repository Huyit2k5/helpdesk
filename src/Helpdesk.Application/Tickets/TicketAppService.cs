using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.CannedResponses;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Permissions;
using Helpdesk.Priorities;
using Helpdesk.Tickets.Dtos;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Linq;

namespace Helpdesk.Tickets;

[Authorize(HelpdeskPermissions.Tickets.Default)]
public class TicketAppService : ApplicationService, ITicketAppService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketComment, Guid> _commentRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<TicketSource, Guid> _sourceRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly TicketManager _ticketManager;

    public TicketAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketComment, Guid> commentRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<TicketSource, Guid> sourceRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<IdentityUser, Guid> userRepository,
        TicketManager ticketManager)
    {
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _activityRepository = activityRepository;
        _categoryRepository = categoryRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _sourceRepository = sourceRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _ticketManager = ticketManager;
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

        if (input.StatusId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.StatusId == input.StatusId.Value);
        }

        if (input.PriorityId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.PriorityId == input.PriorityId.Value);
        }

        if (input.CategoryId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CategoryId == input.CategoryId.Value);
        }

        if (input.DepartmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }

        if (input.AssigneeId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.AssigneeId == input.AssigneeId.Value);
        }

        if (input.SourceId.HasValue)
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

        var comments = await _commentRepository.GetListAsync(c => c.TicketId == id);
        var activities = await _activityRepository.GetListAsync(a => a.TicketId == id);

        var commentCreatorIds = comments.Where(c => c.CreatorId.HasValue).Select(c => c.CreatorId!.Value).Distinct().ToList();
        var activityCreatorIds = activities.Where(a => a.CreatorId.HasValue).Select(a => a.CreatorId!.Value).Distinct().ToList();
        var allUserIds = commentCreatorIds.Union(activityCreatorIds).Distinct().ToList();

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
            Tags = ticket.Tags,
            Comments = comments.OrderBy(c => c.CreationTime).Select(c => new TicketCommentDto
            {
                Id = c.Id,
                CreationTime = c.CreationTime,
                CreatorId = c.CreatorId,
                TicketId = c.TicketId,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatorName = c.CreatorId.HasValue ? userDict.GetValueOrDefault(c.CreatorId.Value) : null
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

        await _ticketRepository.InsertAsync(ticket);

        return await GetAsync(ticket.Id);
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

        await _ticketRepository.UpdateAsync(ticket);

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
        await _ticketRepository.UpdateAsync(ticket);

        return await GetAsync(id);
    }

    [Authorize(HelpdeskPermissions.Tickets.ChangeStatus)]
    public async Task<TicketDetailDto> ChangeStatusAsync(Guid id, ChangeTicketStatusInput input)
    {
        var ticket = await _ticketRepository.GetAsync(id);
        var oldStatus = await _statusRepository.GetAsync(ticket.StatusId);
        var newStatus = await _statusRepository.GetAsync(input.StatusId);

        await _ticketManager.ChangeStatusAsync(ticket, oldStatus, newStatus);
        await _ticketRepository.UpdateAsync(ticket);

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
            CreatorName = c.CreatorId.HasValue ? userDict.GetValueOrDefault(c.CreatorId.Value) : null
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
}
