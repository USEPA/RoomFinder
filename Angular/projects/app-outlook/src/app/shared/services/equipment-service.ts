import { Injectable } from '@angular/core';
import { RoomFinderServiceBase } from './roomfinder-service-base';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { FilterBaseEntity } from '../models/filter-base.model';
import { EquipmentFilterResults } from '../models/resource-equipment.model';
import { AppConfigService } from './app.config.service';
import { LoggingService } from './logging-service';
import { EquipmentAPI, Equipment } from '../models/resource-equipment.model';

@Injectable()
export class EquipmentService extends RoomFinderServiceBase {
  private apiBase: string;
  private equipment: Equipment[] = [];

  constructor(
    private http: HttpClient,
    protected snackBar: MatSnackBar,
    protected configService: AppConfigService,
    protected loggingService: LoggingService
  ) {
    super(snackBar, loggingService);
    const config = this.configService.getConfig();
    this.apiBase = config.auth.baseWebApiUrl + 'api/equipmentDataService/v1.0/';
  }

  public postEquipmentFind(equipmentFilterEntity: FilterBaseEntity): Observable<EquipmentFilterResults[]> {
    const url = `${this.apiBase}find/`;
    const headers = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };
    const filterObject = JSON.stringify(equipmentFilterEntity);
    return this.http
      .post<EquipmentFilterResults[]>(url, filterObject, headers)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getEquipmentFromAPI(): Observable<EquipmentAPI[]> {
    const url = `${this.apiBase}EquipmentLists/`;
    return this.http.get<EquipmentAPI[]>(url)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getEquipmentList(equipmentAPI: EquipmentAPI[]) {
    if (this.equipment.length === 0) {
      equipmentAPI.forEach(element => {
        this.equipment.push({
          value: element.displayName,
          viewValue: element.displayName
        });
      });
    }
    return this.equipment.slice(); // copy
  }

  getEquipmentTypesFromAPI(): Observable<string[]> {
    const url = `${this.apiBase}equipmentTypes/`;
    return this.http.get<string[]>(url)
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }

  getStatesFromAPI(): Observable<string[]> {
    const url = `${this.apiBase}states/`;
    return this.http.get<string[]>(url)
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

  getCitiesFromAPI(state: string): Observable<string[]> {
    const url = `${this.apiBase}cities/`;
    const params = new HttpParams()
      .set('state', state);
    return this.http.get<string[]>(url, { params })
      .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
  }
}
