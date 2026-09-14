import type { DiscordLinkCodeDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DiscordAccountLinkService {
  private restService = inject(RestService);
  apiName = 'Default';

  generateLinkCode = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, DiscordLinkCodeDto>({
      method: 'POST',
      url: '/api/app/discord-account-link/generate-link-code',
    },
    { apiName: this.apiName, ...config });
}
