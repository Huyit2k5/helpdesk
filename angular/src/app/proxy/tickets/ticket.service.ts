import type { AssignTicketInput, ChangeTicketStatusInput, CreateTicketCommentDto, CreateTicketDto, GetTicketListInput, TicketActivityDto, TicketAttachmentDto, TicketCommentDto, TicketDetailDto, TicketListDto, UpdateTicketDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TicketService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  addComment = (id: string, input: CreateTicketCommentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketCommentDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/comment`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  assign = (id: string, input: AssignTicketInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketDetailDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/assign`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  changeStatus = (id: string, input: ChangeTicketStatusInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketDetailDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/change-status`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateTicketDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketDetailDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/ticket/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketDetailDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActivities = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketActivityDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/activities`,
    },
    { apiName: this.apiName,...config });
  

  getComments = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketCommentDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/comments`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTicketListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TicketListDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket',
      params: { filter: input.filter, statusId: input.statusId, priorityId: input.priorityId, categoryId: input.categoryId, departmentId: input.departmentId, assigneeId: input.assigneeId, sourceId: input.sourceId, dateFrom: input.dateFrom, dateTo: input.dateTo, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateTicketDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketDetailDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  uploadAttachment = (id: string, file: File, commentId?: string, config?: Partial<Rest.Config>) => {
    const formData = new FormData();
    formData.append('file', file);
    return this.restService.request<any, TicketAttachmentDto>({
      method: 'POST',
      url: `/api/app/ticket/${id}/upload-attachment`,
      params: commentId ? { commentId } : {},
      body: formData,
    },
    { apiName: this.apiName,...config });
  };

  getAttachments = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketAttachmentDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket/${id}/attachments`,
    },
    { apiName: this.apiName,...config });

  downloadAttachment = (attachmentId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, Blob>({
      method: 'POST',
      responseType: 'blob',
      url: `/api/app/ticket/download-attachment/${attachmentId}`,
    },
    { apiName: this.apiName,...config });

  deleteAttachment = (attachmentId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/ticket/attachment/${attachmentId}`,
    },
    { apiName: this.apiName,...config });

  exportExcel = (input: GetTicketListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, Blob>({
      method: 'POST',
      responseType: 'blob',
      url: '/api/app/ticket/export-excel',
      body: input,
    },
    { apiName: this.apiName,...config });
}