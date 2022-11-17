import { MsalAngularConfiguration } from '@azure/msal-angular';
import { Configuration } from 'msal';

export interface IAppConfig {
  env: {
    name: string;
    version: string;
    referrerIsIE?: boolean;
  };
  auth: {
    baseWebApiUrl: string;
    audience: string;
    clientId: string;
    authority?: string;
    validateAuthority?: boolean;
    redirectUri?: string | (() => string);
    postLogoutRedirectUri?: string | (() => string);
    navigateToLoginRequestUrl?: boolean;
    azureDomain?: string;
  };
}

export interface IMsalConfig {
  msalConfig: Configuration;
  msalAngularConfig: MsalAngularConfiguration;
}
