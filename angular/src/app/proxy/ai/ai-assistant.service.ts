import type {
  AiSettingsDto,
  UpdateAiSettingsDto,
  TestAiConnectionInputDto,
  TestAiConnectionResultDto,
  TicketAiSummaryDto,
  GenerateAiReplyInputDto,
  GenerateAiReplyResultDto,
  AnalyzeSentimentResultDto
} from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AiAssistantService {
  private restService = inject(RestService);
  apiName = 'Default';

  getSettings = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AiSettingsDto>({
      method: 'GET',
      url: '/api/app/ai-assistant/settings',
    },
    { apiName: this.apiName, ...config });

  updateSettings = (input: UpdateAiSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/ai-assistant/settings',
      body: input,
    },
    { apiName: this.apiName, ...config });

  testConnection = (input: TestAiConnectionInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TestAiConnectionResultDto>({
      method: 'POST',
      url: '/api/app/ai-assistant/test-connection',
      body: input,
    },
    { apiName: this.apiName, ...config });

  summarizeTicket = (ticketId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketAiSummaryDto>({
      method: 'POST',
      url: `/api/app/ai-assistant/summarize-ticket/${ticketId}`,
    },
    { apiName: this.apiName, ...config });

  generateReply = (input: GenerateAiReplyInputDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, GenerateAiReplyResultDto>({
      method: 'POST',
      url: '/api/app/ai-assistant/generate-reply',
      body: input,
    },
    { apiName: this.apiName, ...config });

  analyzeTicketSentiment = (ticketId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AnalyzeSentimentResultDto>({
      method: 'POST',
      url: `/api/app/ai-assistant/analyze-ticket-sentiment/${ticketId}`,
    },
    { apiName: this.apiName, ...config });
}
