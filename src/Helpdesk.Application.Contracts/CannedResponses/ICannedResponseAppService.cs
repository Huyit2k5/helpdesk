using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.CannedResponses;

public interface ICannedResponseAppService : ICrudAppService<
    CannedResponseDto,
    Guid,
    CannedResponseGetListInput,
    CreateUpdateCannedResponseDto>
{
}
