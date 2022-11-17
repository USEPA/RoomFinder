import { OnDestroy } from '@angular/core';
import { MatSnackBarConfig, MatSnackBar } from '@angular/material/snack-bar';
import { takeUntil } from 'rxjs/operators';
import { throwError, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HttpErrorResponse } from '@angular/common/http';
import { LoggingService } from './logging-service';

export class RoomFinderServiceBase implements OnDestroy {
  public error: any;
  private infoClass = 'notification-info';
  private failureClass = 'notification-failure';
  private successClass = 'notification-success';
  private unsubscribeAllNotification = new Subject();
  constructor(protected snackBar: MatSnackBar, protected loggingService: LoggingService) { }


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

  ngOnDestroy(): void {
    this.unsubscribeAllNotification.next();
    this.unsubscribeAllNotification.complete();
  }
}
