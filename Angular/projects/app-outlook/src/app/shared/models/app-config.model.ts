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
