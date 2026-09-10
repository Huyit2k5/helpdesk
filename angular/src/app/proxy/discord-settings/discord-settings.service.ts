import type { DiscordSettingsDto, SendTestDiscordInput, TestDiscordResultDto, UpdateDiscordSettingsDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DiscordSettingsService {
  private restService = inject(RestService);
  apiName = 'Default';

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DiscordSettingsDto>({
      method: 'GET',
      url: '/api/app/discord-settings',
    },
    { apiName: this.apiName, ...config });

  update = (input: UpdateDiscordSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/discord-settings',
      body: input,
    },
    { apiName: this.apiName, ...config });

  sendTestNotification = (input: SendTestDiscordInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TestDiscordResultDto>({
      method: 'POST',
      url: '/api/app/discord-settings/send-test-notification',
      body: input,
    },
    { apiName: this.apiName, ...config });
}
