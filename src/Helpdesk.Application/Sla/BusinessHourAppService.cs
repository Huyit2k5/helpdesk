using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Helpdesk.Sla.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Sla;

[Authorize(HelpdeskPermissions.Sla.BusinessHours)]
public class BusinessHourAppService : ApplicationService, IBusinessHourAppService
{
    private readonly IRepository<BusinessHour, Guid> _businessHourRepository;
    private readonly IRepository<Holiday, Guid> _holidayRepository;

    public BusinessHourAppService(
        IRepository<BusinessHour, Guid> businessHourRepository,
        IRepository<Holiday, Guid> holidayRepository)
    {
        _businessHourRepository = businessHourRepository;
        _holidayRepository = holidayRepository;
    }

    public async Task<List<BusinessHourDto>> GetBusinessHoursAsync()
    {
        var hours = await _businessHourRepository.GetListAsync();
        if (hours.Count == 0)
        {
            var defaults = new List<BusinessHour>
            {
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Monday, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), true, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Tuesday, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), true, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Wednesday, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), true, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Thursday, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), true, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Friday, new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0), true, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Saturday, new TimeSpan(8, 30, 0), new TimeSpan(12, 0, 0), false, CurrentTenant.Id),
                new BusinessHour(GuidGenerator.Create(), DayOfWeek.Sunday, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0), false, CurrentTenant.Id),
            };
            await _businessHourRepository.InsertManyAsync(defaults, autoSave: true);
            hours = defaults;
        }

        return hours.OrderBy(h => h.DayOfWeek).Select(h => new BusinessHourDto
        {
            Id = h.Id,
            DayOfWeek = h.DayOfWeek,
            StartTime = h.StartTime.ToString(@"hh\:mm"),
            EndTime = h.EndTime.ToString(@"hh\:mm"),
            IsWorkingDay = h.IsWorkDay
        }).ToList();
    }

    public async Task<List<BusinessHourDto>> UpdateBusinessHoursAsync(List<UpdateBusinessHourDto> input)
    {
        foreach (var item in input)
        {
            var entity = await _businessHourRepository.GetAsync(item.Id);
            entity.StartTime = TimeSpan.Parse(item.StartTime);
            entity.EndTime = TimeSpan.Parse(item.EndTime);
            entity.IsWorkDay = item.IsWorkingDay;
            await _businessHourRepository.UpdateAsync(entity);
        }

        return await GetBusinessHoursAsync();
    }

    public async Task<List<HolidayDto>> GetHolidaysAsync()
    {
        var holidays = await _holidayRepository.GetListAsync();
        return holidays.OrderBy(h => h.Date).Select(h => new HolidayDto
        {
            Id = h.Id,
            Name = h.Name,
            Date = h.Date,
            IsRecurring = h.IsRecurring
        }).ToList();
    }

    public async Task<HolidayDto> CreateHolidayAsync(CreateUpdateHolidayDto input)
    {
        var holiday = new Holiday(
            GuidGenerator.Create(),
            input.Name,
            input.Date,
            input.IsRecurring,
            CurrentTenant.Id);

        await _holidayRepository.InsertAsync(holiday, autoSave: true);

        return new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurring = holiday.IsRecurring
        };
    }

    public async Task<HolidayDto> UpdateHolidayAsync(Guid id, CreateUpdateHolidayDto input)
    {
        var holiday = await _holidayRepository.GetAsync(id);
        holiday.SetName(input.Name);
        holiday.Date = input.Date;
        holiday.IsRecurring = input.IsRecurring;

        await _holidayRepository.UpdateAsync(holiday, autoSave: true);

        return new HolidayDto
        {
            Id = holiday.Id,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurring = holiday.IsRecurring
        };
    }

    public async Task DeleteHolidayAsync(Guid id)
    {
        await _holidayRepository.DeleteAsync(id, autoSave: true);
    }
}
