import { Injectable } from '@angular/core';
import { RoomFinderServiceBase } from './roomfinder-service-base';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppConfigService } from './app.config.service';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RoomFilterEntity, RoomFilterResults } from '../models/resource-room.model';
import { LoggingService } from './logging-service';
import { AvailabilityRequestFilterEntity, AvailabilityRequestFilterAPI } from '../models/availabilityrequests.model';
import { LocationsAPI } from '../models/locations.model';

@Injectable()
export class RoomService extends RoomFinderServiceBase {
  private apiBase: string;

  constructor(
    private http: HttpClient,
    protected snackBar: MatSnackBar,
    protected configService: AppConfigService,
    protected loggingService: LoggingService
  ) {
    super(snackBar, loggingService);
    const config = this.configService.getConfig();
    this.apiBase = `${config.auth.baseWebApiUrl}api/roomDataService/v1.0/`;
  }

  public postFindRoomAvailability(roomFilterEntity: RoomFilterEntity): Observable<RoomFilterResults[]> {
    const url = `${this.apiBase}find/`;
    const headers = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };
    const params = JSON.stringify(roomFilterEntity);
    return this.http
      .post<RoomFilterResults[]>(url, params, headers)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getStatesFromAPI(): Observable<string[]> {
    const url = `${this.apiBase}states/`;
    return this.http.get<string[]>(url)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getCitiesFromAPI(state: string): Observable<string[]> {
    const url = `${this.apiBase}cities/`;
    const params = new HttpParams()
      .set('state', state);
    return this.http.get<string[]>(url, { params })
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getOfficesFromAPI(state: string, city: string): Observable<string[]> {
    const url = `${this.apiBase}officees/`;
    const params = new HttpParams()
      .set('state', state)
      .set('city', city);
    return this.http.get<string[]>(url, { params })
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getFloorsFromAPI(state: string, city: string, office: string): Observable<string[]> {
    const url = `${this.apiBase}floors/`;
    const params = new HttpParams()
      .set('state', state)
      .set('city', city)
      .set('office', office);
    return this.http
      .get<string[]>(url, { params })
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  public getCheckRecurrence(availablityRequestFilterEntity: AvailabilityRequestFilterEntity):
    Observable<AvailabilityRequestFilterAPI[]> {
    const url = `${this.apiBase}checkRecurrence/`;
    const headers = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };
    let params = '';
    params = JSON.stringify(availablityRequestFilterEntity);
    return this.http
      .post<AvailabilityRequestFilterAPI[]>(url, params, headers)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getLocationsFromAPI(): Observable<LocationsAPI[]> {
    const url = `${this.apiBase}roomLocations/`;
    return this.http.get<LocationsAPI[]>(url)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }
}
