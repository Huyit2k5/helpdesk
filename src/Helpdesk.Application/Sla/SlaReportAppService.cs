using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Helpdesk.Sla.Dtos;
using Helpdesk.Tickets;
using Microsoft.AspNetCore.Authorization;
using MiniExcelLibs;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Sla;

[Authorize(HelpdeskPermissions.Sla.Reports)]
public class SlaReportAppService : ApplicationService, ISlaReportAppService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<SlaBreachLog, Guid> _breachLogRepository;

    public SlaReportAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<SlaBreachLog, Guid> breachLogRepository)
    {
        _ticketRepository = ticketRepository;
        _breachLogRepository = breachLogRepository;
    }

    public async Task<SlaComplianceStatsDto> GetComplianceStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = await _ticketRepository.GetQueryableAsync();
        query = query.Where(t => t.SlaPolicyId != null);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreationTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreationTime <= endDate.Value);
        }

        var tickets = await AsyncExecuter.ToListAsync(query);

        var total = tickets.Count;
        var firstResponseBreached = tickets.Count(t => t.IsFirstResponseBreached);
        var firstResponseMet = total - firstResponseBreached;

        var resolutionBreached = tickets.Count(t => t.IsResolutionBreached);
        var resolutionMet = total - resolutionBreached;

        return new SlaComplianceStatsDto
        {
            TotalTicketsWithSla = total,
            FirstResponseMetCount = firstResponseMet,
            FirstResponseBreachedCount = firstResponseBreached,
            ResolutionMetCount = resolutionMet,
            ResolutionBreachedCount = resolutionBreached
        };
    }

    public async Task<PagedResultDto<SlaBreachLogDto>> GetBreachLogsAsync(GetSlaBreachListInput input)
    {
        var breachQuery = await _breachLogRepository.GetQueryableAsync();
        var ticketQuery = await _ticketRepository.GetQueryableAsync();

        if (input.BreachType.HasValue)
        {
            breachQuery = breachQuery.Where(b => b.BreachType == input.BreachType.Value);
        }

        if (input.StartDate.HasValue)
        {
            breachQuery = breachQuery.Where(b => b.CreationTime >= input.StartDate.Value);
        }

        if (input.EndDate.HasValue)
        {
            breachQuery = breachQuery.Where(b => b.CreationTime <= input.EndDate.Value);
        }

        var joinedQuery = from b in breachQuery
                          join t in ticketQuery on b.TicketId equals t.Id into tj
                          from t in tj.DefaultIfEmpty()
                          select new
                          {
                              Breach = b,
                              TicketNumber = t != null ? t.TicketNumber : string.Empty,
                              TicketTitle = t != null ? t.Title : string.Empty
                          };

        var totalCount = await AsyncExecuter.CountAsync(joinedQuery);

        var sorting = string.IsNullOrWhiteSpace(input.Sorting)
            ? "Breach.CreationTime desc"
            : (input.Sorting.StartsWith("Breach.") ? input.Sorting : "Breach." + input.Sorting);

        var items = await AsyncExecuter.ToListAsync(
            joinedQuery.OrderBy(sorting).PageBy(input));

        var dtos = items.Select(x => new SlaBreachLogDto
        {
            Id = x.Breach.Id,
            CreationTime = x.Breach.CreationTime,
            CreatorId = x.Breach.CreatorId,
            TicketId = x.Breach.TicketId,
            TicketNumber = x.TicketNumber,
            TicketTitle = x.TicketTitle,
            BreachType = x.Breach.BreachType,
            ExpectedDate = x.Breach.ExpectedAt,
            ActualDate = x.Breach.ResolvedOrRespondedAt ?? x.Breach.BreachedAt,
            BreachedMinutes = x.Breach.ElapsedMinutes ?? 0,
            Reason = x.Breach.Description
        }).ToList();

        return new PagedResultDto<SlaBreachLogDto>(totalCount, dtos);
    }

    public async Task<IRemoteStreamContent> ExportBreachesExcelAsync(GetSlaBreachListInput input)
    {
        var breachQuery = await _breachLogRepository.GetQueryableAsync();
        var ticketQuery = await _ticketRepository.GetQueryableAsync();

        if (input.BreachType.HasValue)
        {
            breachQuery = breachQuery.Where(b => b.BreachType == input.BreachType.Value);
        }

        if (input.StartDate.HasValue)
        {
            breachQuery = breachQuery.Where(b => b.CreationTime >= input.StartDate.Value);
        }

        if (input.EndDate.HasValue)
        {
            var end = input.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            breachQuery = breachQuery.Where(b => b.CreationTime <= end);
        }

        var joinedQuery = from b in breachQuery
                          join t in ticketQuery on b.TicketId equals t.Id into tj
                          from t in tj.DefaultIfEmpty()
                          select new
                          {
                              Breach = b,
                              TicketNumber = t != null ? t.TicketNumber : string.Empty,
                              TicketTitle = t != null ? t.Title : string.Empty
                          };

        var items = await AsyncExecuter.ToListAsync(joinedQuery.OrderByDescending(x => x.Breach.CreationTime));

        var exportData = items.Select((x, index) => new Dictionary<string, object?>
        {
            ["STT"] = index + 1,
            ["Mã Sự Vụ"] = x.TicketNumber,
            ["Tiêu Đề"] = x.TicketTitle,
            ["Loại Vi Phạm"] = x.Breach.BreachType == SlaBreachType.Response ? "Phản hồi đầu tiên" : "Thời gian giải quyết",
            ["Hạn Định"] = x.Breach.ExpectedAt.ToString("dd/MM/yyyy HH:mm"),
            ["Thực Tế Xử Lý"] = (x.Breach.ResolvedOrRespondedAt ?? x.Breach.BreachedAt).ToString("dd/MM/yyyy HH:mm"),
            ["Trễ (Phút)"] = x.Breach.ElapsedMinutes ?? 0,
            ["Mô Tả / Lý Do"] = x.Breach.Description ?? ""
        }).ToList();

        var memoryStream = new MemoryStream();
        await memoryStream.SaveAsAsync(exportData);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var fileName = $"SLA_ViPham_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
