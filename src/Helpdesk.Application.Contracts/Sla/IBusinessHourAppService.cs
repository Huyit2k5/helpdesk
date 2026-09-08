using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Sla.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Sla;

public interface IBusinessHourAppService : IApplicationService
{
    Task<List<BusinessHourDto>> GetBusinessHoursAsync();
    Task<List<BusinessHourDto>> UpdateBusinessHoursAsync(List<UpdateBusinessHourDto> input);
    Task<List<HolidayDto>> GetHolidaysAsync();
    Task<HolidayDto> CreateHolidayAsync(CreateUpdateHolidayDto input);
    Task<HolidayDto> UpdateHolidayAsync(Guid id, CreateUpdateHolidayDto input);
    Task DeleteHolidayAsync(Guid id);
}
