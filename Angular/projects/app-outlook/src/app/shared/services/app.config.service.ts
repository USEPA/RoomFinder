import { Injectable } from '@angular/core';
import { IAppConfig } from '../models/app-config.model';
import { HttpClient, HttpBackend } from '@angular/common/http';
import { IEnvironment } from '../models/environment.model';
import { environment } from '../../../environments/environment';

let appConfig: IAppConfig = null;

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
        return appConfig;
      });
    return promise;
  }

  getConfig(): IAppConfig {
    return appConfig;
  }

  getBaseUri(): string {
    return this.getConfig().auth.baseWebApiUrl;
  }
}
