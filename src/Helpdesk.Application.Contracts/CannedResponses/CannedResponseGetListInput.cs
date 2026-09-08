using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.CannedResponses;

public class CannedResponseGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsPublic { get; set; }
}
