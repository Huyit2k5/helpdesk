using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Tickets.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Tickets;

public interface ITicketAppService : IApplicationService
{
    Task<PagedResultDto<TicketListDto>> GetListAsync(GetTicketListInput input);

    Task<TicketDetailDto> GetAsync(Guid id);

    Task<TicketDetailDto> CreateAsync(CreateTicketDto input);

    Task<TicketDetailDto> UpdateAsync(Guid id, UpdateTicketDto input);

    Task DeleteAsync(Guid id);

    Task<TicketDetailDto> AssignAsync(Guid id, AssignTicketInput input);

    Task<TicketDetailDto> ChangeStatusAsync(Guid id, ChangeTicketStatusInput input);

    Task<TicketCommentDto> AddCommentAsync(Guid id, CreateTicketCommentDto input);

    Task<List<TicketCommentDto>> GetCommentsAsync(Guid id);

    Task<List<TicketActivityDto>> GetActivitiesAsync(Guid id);

    Task<TicketAttachmentDto> UploadAttachmentAsync(Guid id, Volo.Abp.Content.IRemoteStreamContent file, Guid? commentId = null);

    Task<List<TicketAttachmentDto>> GetAttachmentsAsync(Guid id);

    Task<Volo.Abp.Content.IRemoteStreamContent> DownloadAttachmentAsync(Guid attachmentId);

    Task DeleteAttachmentAsync(Guid attachmentId);

    Task<Volo.Abp.Content.IRemoteStreamContent> ExportExcelAsync(GetTicketListInput input);
}
