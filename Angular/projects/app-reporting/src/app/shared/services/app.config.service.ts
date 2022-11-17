import { Injectable } from '@angular/core';
import { IAppConfig } from '../models/app-config.model';
import { HttpClient, HttpBackend } from '@angular/common/http';
import { MsalAngularConfiguration } from '@azure/msal-angular';
import { Configuration } from 'msal';
import { IEnvironment } from '../models/environment.model';
import { environment } from '../../../environments/environment';

let appConfig: IAppConfig = null;
let msalConfig: Configuration = null;
let msalAngularConfig: MsalAngularConfiguration = null;

export function msalConfigFactory(): Configuration {
  if (msalConfig === null) {
    return null;
  }
  return msalConfig;
}

export function msalConfigAngularFactory(): MsalAngularConfiguration {
  if (msalAngularConfig === null) {
    return null;
  }
  return msalAngularConfig;
}

export function appConfigFactory(configService: AppConfigService) {
  return async () => {
    const isIE = window.navigator.userAgent.indexOf('MSIE ') > -1 || window.navigator.userAgent.indexOf('Trident/') > -1;
    return configService.load(environment, isIE).then(() => { });
  };
}

@Injectable()
export class AppConfigService {
  http: HttpClient;
  constructor(httpBackend: HttpBackend) {
    this.http = new HttpClient(httpBackend);
  }

  load(env: IEnvironment, isIE: boolean = false): Promise<IAppConfig> {
    let baseUri = location.origin + '/';
    if (!!env.baseApiUrl) {
      baseUri = env.baseApiUrl;
    }
    const promise = this.http.get<IAppConfig>(`${baseUri}api/clientsettings/${env.name}?isIE=${isIE}`).toPromise()
      .then(data => {
        appConfig = data;

        const redirectUri = location.origin;
        const defaultScopes = ['user.read', 'calendars.read', 'calendars.read.shared'];

        const protectedMap: [string, string[]][] = [
          [`${data.auth.baseWebApiUrl}api/reporting`, [data.auth.audience]],
          [`${data.auth.baseWebApiUrl}swagger/v2.0/swagger.json`, [data.auth.audience]],
          ['https://graph.microsoft.com/v1.0/me', defaultScopes]];

        const unprotectedResourceMap: string[] = [
          `${data.auth.baseWebApiUrl}v1.0/logs`,
          `${data.auth.baseWebApiUrl}api/roomDataService`,
          `${data.auth.baseWebApiUrl}api/equipmentDataService`,
          `${data.auth.baseWebApiUrl}api/clientsettings`
        ];

        msalConfig = {
          auth: {
            clientId: data.auth.clientId,
            authority: data.auth.authority,
            redirectUri,
            postLogoutRedirectUri: redirectUri + '/'
          }
        };

        msalAngularConfig = {
          popUp: !isIE,
          consentScopes: defaultScopes,
          protectedResourceMap: protectedMap,
          unprotectedResources: unprotectedResourceMap,
          extraQueryParameters: {}
        };
        return appConfig;
      });
    return promise;
  }

  getConfig(): IAppConfig {
    return appConfig;
  }

  getBaseUri(): string {
    return appConfig.auth.baseWebApiUrl;
  }

  getVersion(): string {
    return appConfig.env.version;
  }
}
