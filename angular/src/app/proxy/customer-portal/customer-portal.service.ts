import type { AddCustomerCommentDto, CreateCustomerTicketDto, CustomerCommentDto, CustomerTicketDetailDto, CustomerTicketDto, GetCustomerTicketListInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CustomerPortalService {
  private restService = inject(RestService);
  apiName = 'Default';

  getMyTickets = (input: GetCustomerTicketListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CustomerTicketDto>>({
      method: 'GET',
      url: '/api/app/customer-portal/my-tickets',
      params: { filter: input.filter, categoryId: input.categoryId, isClosed: input.isClosed, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName, ...config });

  getMyTicket = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerTicketDetailDto>({
      method: 'GET',
      url: `/api/app/customer-portal/my-ticket/${id}`,
    },
    { apiName: this.apiName, ...config });

  createMyTicket = (input: CreateCustomerTicketDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerTicketDetailDto>({
      method: 'POST',
      url: '/api/app/customer-portal/my-ticket',
      body: input,
    },
    { apiName: this.apiName, ...config });

  addMyComment = (ticketId: string, input: AddCustomerCommentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerCommentDto>({
      method: 'POST',
      url: `/api/app/customer-portal/my-ticket/${ticketId}/comment`,
      body: input,
    },
    { apiName: this.apiName, ...config });
}
