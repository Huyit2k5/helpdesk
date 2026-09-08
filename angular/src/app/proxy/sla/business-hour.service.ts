import type { BusinessHourDto, CreateUpdateHolidayDto, HolidayDto, UpdateBusinessHourDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class BusinessHourService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  createHoliday = (input: CreateUpdateHolidayDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HolidayDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/business-hour/holiday',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deleteHoliday = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/business-hour/${id}/holiday`,
    },
    { apiName: this.apiName,...config });
  

  getBusinessHours = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, BusinessHourDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/business-hour/business-hours',
    },
    { apiName: this.apiName,...config });
  

  getHolidays = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, HolidayDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/business-hour/holidays',
    },
    { apiName: this.apiName,...config });
  

  updateBusinessHours = (input: readonly UpdateBusinessHourDto[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, BusinessHourDto[]>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: '/api/app/business-hour/business-hours',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateHoliday = (id: string, input: CreateUpdateHolidayDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HolidayDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/business-hour/${id}/holiday`,
      body: input,
    },
    { apiName: this.apiName,...config });
}