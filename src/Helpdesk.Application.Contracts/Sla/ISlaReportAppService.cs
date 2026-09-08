using System;
using System.Threading.Tasks;
using Helpdesk.Sla.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Sla;

public interface ISlaReportAppService : IApplicationService
{
    Task<SlaComplianceStatsDto> GetComplianceStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PagedResultDto<SlaBreachLogDto>> GetBreachLogsAsync(GetSlaBreachListInput input);
}
