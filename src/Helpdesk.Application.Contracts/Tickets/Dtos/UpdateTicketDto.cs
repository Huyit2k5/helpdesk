using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Tickets.Dtos;

public class UpdateTicketDto
{
    [Required]
    [StringLength(TicketConsts.MaxTitleLength)]
    public string Title { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid PriorityId { get; set; }

    [Required]
    public Guid StatusId { get; set; }

    [Required]
    public Guid SourceId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? AssigneeId { get; set; }

    [Required]
    [StringLength(TicketConsts.MaxRequesterNameLength)]
    public string RequesterName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(TicketConsts.MaxRequesterEmailLength)]
    public string RequesterEmail { get; set; } = null!;

    [StringLength(TicketConsts.MaxRequesterPhoneLength)]
    public string? RequesterPhone { get; set; }

    public DateTime? DueDate { get; set; }

    [StringLength(TicketConsts.MaxTagsLength)]
    public string? Tags { get; set; }

    public Guid? AssetId { get; set; }
}
