using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Dashboard.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<DashboardStatsDto> GetStatsAsync(GetDashboardInput input);
    Task<List<TicketTrendItemDto>> GetTicketTrendAsync(GetDashboardInput input);
    Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(GetDashboardInput input);
}
