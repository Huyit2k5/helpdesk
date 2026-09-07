import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44346/',
  redirectUri: baseUrl,
  clientId: 'Helpdesk_App',
  responseType: 'code',
  scope: 'offline_access Helpdesk',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'Helpdesk',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44346',
      rootNamespace: 'Helpdesk',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
