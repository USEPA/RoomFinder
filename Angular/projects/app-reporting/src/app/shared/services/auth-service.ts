import { Injectable } from '@angular/core';
import { MsalService, BroadcastService, } from '@azure/msal-angular';
import { Logger, CryptoUtils } from 'msal';
import { AlertsService } from './alerts-service';
import { AppConfigService } from './app.config.service';
import { Event } from '../models/event.model';
import { User } from '../models/user.model';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';
import * as MicrosoftGraph from '@microsoft/microsoft-graph-types';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  public authenticated: boolean;
  public user: User = null;
  GRAPH_ENDPOINT = 'https://graph.microsoft.com/v1.0';

  constructor(
    private http: HttpClient,
    private broadcastService: BroadcastService,
    private msalService: MsalService,
    private alertsService: AlertsService,
    protected configService: AppConfigService) {

    this.checkoutAccount();

    this.broadcastService.subscribe('msal:loginSuccess', () => {
      this.checkoutAccount();
    });

    this.msalService.handleRedirectCallback((authError, response) => {
      if (authError) {
        console.error('Redirect Error: ', authError.errorMessage);
        this.alertsService.add('Login failed', JSON.stringify(authError.errorMessage, null, 2));
        return;
      }

      console.log('Redirect Success: ', response.accessToken);
      this.getCurrentUserAndSetSubjects();
    });

    this.msalService.setLogger(new Logger(
      (logLevel, message, piiEnabled) => console.log(`MSAL Logging: ${message} Level:${logLevel} pii:${piiEnabled}`),
      {
        correlationId: CryptoUtils.createNewGuid(),
        piiLoggingEnabled: false
      }));

    this.getCurrentUserAndSetSubjects();
  }

  signIn() {
    const isIE = window.navigator.userAgent.indexOf('MSIE ') > -1 || window.navigator.userAgent.indexOf('Trident/') > -1;
    if (isIE) {
      this.msalService.loginRedirect();
    } else {
      this.msalService
        .loginPopup()
        .catch(reason => {
          this.alertsService.add('Login failed', JSON.stringify(reason, null, 2));
        }).then((val: any) => {
          console.log(`loginPopup returned with ${val}`);
          this.getCurrentUserAndSetSubjects();
        });
    }
  }

  signOut(): void {
    this.msalService.logout();
    this.user = null;
    this.authenticated = false;
  }

  isAuthenticated(): boolean {
    this.checkoutAccount();
    return (this.authenticated);
  }

  private getCurrentUserAndSetSubjects() {
    if (this.isAuthenticated() && !(!!this.user)) {
      console.log('User authenticated but user variable nullified');
      const account = this.msalService.getAccount();
      const accountuser = new User();
      accountuser.displayName = account.name;
      accountuser.emailAddress = account.userName;
      this.user = accountuser;
    }
  }

  checkoutAccount() {
    this.authenticated = !!this.msalService.getAccount();
  }

  userDisplayName() {
    if (this.isAuthenticated() && !!this.user) {
      return this.user.displayName;
    }
    return '';
  }

  userEmail() {
    if (this.isAuthenticated() && !!this.user) {
      return this.user.emailAddress;
    }
    return '';
  }

  getOutlookEndpoint() {
    return `https://outlook.com/${this.configService.getConfig().auth.azureDomain}?path=/calendar/view/WorkWeek`;
  }


  getUser(): Observable<User> {
    this.checkoutAccount();
    if (!this.authenticated) { return null; }

    return this.http.get(`${this.GRAPH_ENDPOINT}/me`)
      .pipe(map((graphResult: any) => {

        const graphUser: MicrosoftGraph.User = graphResult;

        const user = new User();
        user.displayName = graphUser.displayName || graphUser.mail;
        user.emailAddress = graphUser.mail || graphUser.userPrincipalName;
        user.avatar = graphResult.avatar || graphUser.mail;

        return user;

      }));
  }

  getEvents(): Observable<Event[]> {
    let url = `${this.GRAPH_ENDPOINT}/me/events`;
    url = `${url}?$select=subject,organizer,start,end&$orderby=createdDateTime desc`;
    return this.http.get(url)
      .pipe(catchError(e => this.handleError(e, 'Could not get events')))
      .pipe(map((graphResult: any) => {
        const events: MicrosoftGraph.Event[] = graphResult.value;
        return events;
      }));
  }

  getRoomCalendar(principal: string, startDateTime: string, endDateTime: string): Observable<Event[]> {
    let url = `${this.GRAPH_ENDPOINT}/users/${principal}/calendarView?startDateTime=${startDateTime}&endDateTime=${endDateTime}`;
    url = `${url}?$select=location,organizer,start,end,subject&$orderby=createdDateTime desc`;
    return this.http.get(url)
      .pipe(catchError(e => this.handleError(e, 'Could not get room calendar events')))
      .pipe(map((graphResult: any) => {
        const events: MicrosoftGraph.Event[] = graphResult.value;
        return events;
      }));
  }

  private handleError(e: HttpErrorResponse, message: string): Observable<never> {
    this.alertsService.add(message, JSON.stringify(e, null, 2));
    return throwError(e.message);
  }
}
