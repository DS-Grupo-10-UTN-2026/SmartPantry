import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44351/',
  redirectUri: baseUrl,
  clientId: 'SmartPantry_App',
  responseType: 'code',
  scope: 'offline_access SmartPantry',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'SmartPantry',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44351',
      rootNamespace: 'SmartPantry',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
