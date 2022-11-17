import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { Observable, throwError, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { catchError } from 'rxjs/operators';
import { AppConfigService } from './app.config.service';
import { LoggingService } from './logging-service';
import { RoomEventsAPI, RoomEventsEntity, MeetingTimeWindowModel, RoomAnalyticsAPI } from '../models/report.model';
import { EquipmentAnalyticsAPI } from '../models/report.model';
import { ResourcesCoreEntity } from '../models/report.model';
import { environment } from '../../../environments/environment';

@Injectable()
export class ReportingService implements OnDestroy {
    private apiBase: string;
    public error: any;
    private infoClass = 'notification-info';
    private failureClass = 'notification-failure';
    private successClass = 'notification-success';
    private unsubscribeAllNotification = new Subject();
    constructor(
        private http: HttpClient,
        protected snackBar: MatSnackBar,
        protected configService: AppConfigService,
        protected loggingService: LoggingService) {
        const config = this.configService.getConfig();
        this.apiBase = config.auth.baseWebApiUrl + 'api/reporting/';
    }

    public get apiSpecificationUrl(): string {
        return `${this.configService.getConfig().auth.baseWebApiUrl}swagger/v2.0/swagger.json`;
    }

    public getApiSpecification(): Observable<Blob> {
        return this.http.get(this.apiSpecificationUrl, {
            responseType: 'blob',
        });
    }

    getRoomsFromAPI(): Observable<ResourcesCoreEntity[]> {
        const url = `${this.apiBase}rooms/`;
        return this.http.get<ResourcesCoreEntity[]>(url)
            .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
    }


    public getReportRoomEventsFromAPI(roomFilterEntity: RoomEventsEntity): Observable<RoomEventsAPI[]> {
        const url = `${this.apiBase}roomEvents/`;
        const headers = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            })
        };
        let params = '';
        params = JSON.stringify(roomFilterEntity);
        console.log(`Params: ${params}`);
        return this.http
            .post<RoomEventsAPI[]>(url, params, headers)
            .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
    }

    public getReportRoomAnalyticsFromAPI(roomFilterEntity: MeetingTimeWindowModel): Observable<RoomAnalyticsAPI[]> {
        const url = `${this.apiBase}roomAnalytics/`;
        const headers = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            })
        };
        let params = '';
        params = JSON.stringify(roomFilterEntity);
        console.log(`Params: ${params}`);
        return this.http
            .post<RoomAnalyticsAPI[]>(url, params, headers)
            .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
    }

    public getReportEquipmentAnalyticsFromAPI(equipmentFilterEntity: MeetingTimeWindowModel): Observable<EquipmentAnalyticsAPI[]> {
        const url = `${this.apiBase}EquipmentAnalytics/`;
        const headers = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json'
            })
        };
        let params = '';
        params = JSON.stringify(equipmentFilterEntity);
        console.log(`Params: ${params}`);
        return this.http
            .post<EquipmentAnalyticsAPI[]>(url, params, headers)
            .pipe(catchError(e => this.handleServerErrorWithNotification(e)));
    }

    showThrottlingNotification() {
        this.snackBar.open('You have requested too much data from the server.  Please wait and try again later.', '',
            this.getSnackBarOptions(this.infoClass));
    }

    showSuccessNotification(message: string, options?: MatSnackBarConfig) {
        options = options != null ? options : this.getSnackBarOptions(this.successClass);
        this.snackBar.open(message, '', options);
    }

    showFailureNotification(message?: string, options?: MatSnackBarConfig) {
        if (!message || message.trim().length <= 0) {
            message = 'An error occurred.';
        }
        options = options != null ? options : this.getSnackBarOptions(this.failureClass);
        this.snackBar.open(message, '', options);
    }

    showServerFailureNotification() {
        this.snackBar.open('Server call was unsuccessful.  Please refresh.', 'Refresh Page', this.getSnackBarOptions(this.infoClass));
        if (this.snackBar._openedSnackBarRef) {
            this.snackBar._openedSnackBarRef.onAction().pipe(takeUntil(this.unsubscribeAllNotification)).subscribe(() => {
                location.reload();
            });
        }
    }

    getSnackBarOptions(className: string, autoDismiss = true, dismissTimeout = 3000): MatSnackBarConfig {
        const config: MatSnackBarConfig = {
            horizontalPosition: 'center',
            verticalPosition: 'top',
            panelClass: [className]
        };
        if (autoDismiss) {
            let duration = dismissTimeout;
            if (environment.snackbarDuration) {
                duration = environment.snackbarDuration;
            }

            config.duration = duration;
        }

        return config;
    }

    public handleServerErrorWithNotification(e: HttpErrorResponse, url?: string) {
        if (e.status === 429) {
            this.showThrottlingNotification();
        } else if (e.statusText !== 'OK' && e.status !== 404) {
            this.showServerFailureNotification();
        }
        let message = `Status: ${e.status}\n`;
        message += `| Message: ${e.message}\n`;
        message += `| url: ${url}`;

        this.snackBar.open(message, 'Refresh Page', this.getSnackBarOptions(this.infoClass));
        this.loggingService.logError(message,
            ['handleServerErrorWithNotification', `url:${url}`]);
        return throwError({ message: e.message, status: e.status });
    }

    formatTime(selectedTime: any): string {
        let time = '';
        if (
            selectedTime !== undefined &&
            selectedTime !== null
        ) {
            let selectedHour = selectedTime.hour;
            let selectedMinute = selectedTime.minute;
            if (selectedHour < 10) {
                selectedHour = '0' + selectedHour;
            }
            if (selectedMinute < 10) {
                selectedMinute = '0' + selectedMinute;
            }
            time = selectedHour + ':' + selectedMinute;
        }
        return time;
    }

    public setTimeStartDay(dateTime: Date): Date {
        dateTime.setHours(0);
        dateTime.setMinutes(0);
        return dateTime;
    }

    public setTimeEndDay(dateTime: Date): Date {
        dateTime.setHours(23);
        dateTime.setMinutes(59);
        return dateTime;
    }

    ngOnDestroy(): void {
        this.unsubscribeAllNotification.next();
        this.unsubscribeAllNotification.complete();
    }
}
