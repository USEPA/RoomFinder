import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConfigService } from './app.config.service';
import { Observable, Subject, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LogEntry, LogEntryLevel, LogEntryType } from '../models/loggingapi.model';

@Injectable()
export class LoggingService implements OnDestroy {
    private apiBase: string;
    private unsubscribeAllNotification = new Subject();

    constructor(
        private http: HttpClient,
        protected configService: AppConfigService
    ) {
        const config = this.configService.getConfig();
        this.apiBase = config.auth.baseWebApiUrl + 'v1.0/logs/';
    }

    private addLogEntry(logEntry: LogEntry): Observable<LogEntry> {
        return this.http
            .post<LogEntry>(this.apiBase, logEntry)
            .pipe(catchError(e => this.errHandler(e, logEntry)));
    }

    private createLogItem(operation: string, logType: LogEntryType, infoType: LogEntryLevel, operationProperties?: string[], url?: string) {
        const entry = new LogEntry();
        entry.operation = operation;
        entry.logLevel = infoType;
        entry.logType = logType;
        entry.operationProperties = operationProperties ? operationProperties : [];
        entry.url = url;
        return entry;
    }

    public logTelemetry(operation: string, operationProperties?: string[]): Observable<LogEntry> {
        const log = this.createLogItem(operation, LogEntryType.Telemetry, LogEntryLevel.Information, operationProperties);
        return this.addLogEntry(log);
    }

    public logError(operation: string, operationProperties?: string[]): Observable<LogEntry> {
        const log = this.createLogItem(operation, LogEntryType.Audit, LogEntryLevel.Error, operationProperties);
        return this.addLogEntry(log);
    }

    public logWarning(operation: string, operationProperties?: string[]): Observable<LogEntry> {
        const log = this.createLogItem(operation, LogEntryType.Audit, LogEntryLevel.Warning, operationProperties);
        return this.addLogEntry(log);
    }

    public logInformation(operation: string, operationProperties?: string[]): Observable<LogEntry> {
        const log = this.createLogItem(operation, LogEntryType.Audit, LogEntryLevel.Information, operationProperties);
        return this.addLogEntry(log);
    }

    errHandler(err: any, log: LogEntry): Observable<LogEntry> {
        if (!!err.message) {
            console.error(err.message);
        } else {
            console.error(err);
        }
        return of(log);
    }

    ngOnDestroy(): void {
        this.unsubscribeAllNotification.next();
        this.unsubscribeAllNotification.complete();
    }
}
