using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Helpdesk.CustomerPortal.Dtos;
using Helpdesk.Tickets.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace Helpdesk.CustomerPortal;

public interface ICustomerPortalAppService : IApplicationService
{
    Task<PagedResultDto<CustomerTicketDto>> GetMyTicketsAsync(GetCustomerTicketListInput input);

    Task<CustomerTicketDetailDto> GetMyTicketAsync(Guid id);

    Task<CustomerTicketDetailDto> CreateMyTicketAsync(CreateCustomerTicketDto input);

    Task<CustomerCommentDto> AddMyCommentAsync(Guid ticketId, AddCustomerCommentDto input);

    Task<TicketAttachmentDto> UploadMyAttachmentAsync(Guid ticketId, IRemoteStreamContent file, Guid? commentId = null);

    Task<IRemoteStreamContent> DownloadMyAttachmentAsync(Guid attachmentId);

    Task<CustomerTicketDetailDto> SubmitTicketFeedbackAsync(Guid ticketId, SubmitTicketFeedbackDto input);

    Task<List<AssetDto>> GetMyAssetsAsync();
    Task ConfirmAssetHandoverAsync(Guid assetId, ConfirmAssetHandoverDto input);
}
