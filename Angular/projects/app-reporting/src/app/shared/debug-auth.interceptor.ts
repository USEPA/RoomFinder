import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpRequest, HttpErrorResponse, HttpInterceptor } from '@angular/common/http';
import { Observable, from } from 'rxjs';
import { BroadcastService, MsalService } from '@azure/msal-angular';
import { AuthResponse } from 'msal';
import { mergeMap, tap } from 'rxjs/operators';
import { LoggingService } from './services/logging-service';

@Injectable()
export class DebugAuthInterceptor implements HttpInterceptor {
  constructor(private auth: MsalService, private broadcastService: BroadcastService, private loggingService: LoggingService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const scopes = this.auth.getScopesForEndpoint(req.url);
    this.auth.getLogger().verbose('Url: ' + req.url + ' maps to scopes: ' + scopes);

    // If there are no scopes set for this request, do nothing.
    if (!scopes) {
      return next.handle(req);
    }

    let accessToken: string;

    // Acquire a token for this request, and attach as proper auth header.
    return from(
      this.auth.acquireTokenSilent({ scopes })
        .then((response: AuthResponse) => {
          accessToken = response.accessToken;
          const authHeader = `Bearer ${response.accessToken}`;
          return req.clone({
            setHeaders: {
              Authorization: authHeader,
            }
          });
        })
    )
      .pipe(
        mergeMap(nextReq => next.handle(nextReq)),
        tap(
          event => {
            this.loggingService.logError(`authinterceptor ${event}`);
          },
          err => {
            if (err instanceof HttpErrorResponse && err.status === 401) {
              this.auth.clearCacheForScope(accessToken);
              this.broadcastService.broadcast('msal:notAuthorized', err.message);
            }
          }
        )
      );
  }
}
