import { OnInit, NgZone } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ResourceStatusFilterService } from '../shared/services/resource-status-filter-service';


export class ResourceComponentBase implements OnInit {

  public columnsToDisplay: string[] = [];

  // Progress Spinner Config
  public color = 'primary';
  public mode = 'indeterminate';
  public value = 50;
  public displayProgressSpinner = false;
  public spinnerWithoutBackdrop = false;

  // internal
  includeUnavailable = false;
  includeRestricted = false;

  constructor(
    public zone: NgZone,
    private snackBar: MatSnackBar,
    private resourceStatusFilterService: ResourceStatusFilterService,
  ) { }


  ngOnInit() {
  }

  /**
   * mat-expansion-panel open vs close
   */
  changePanelState(): void {
  }


  checkResourceStatus(resourceStatusFilter): void {
    this.includeUnavailable = false;
    this.includeRestricted = false;
    resourceStatusFilter.forEach(element => {
      if (element.checked) {
        if (
          element.value === this.resourceStatusFilterService.includeUnavailable
        ) {
          this.includeUnavailable = true;
        } else if (
          element.value === this.resourceStatusFilterService.includeRestricted
        ) {
          this.includeRestricted = true;
        }
      }
    });
  }

  protected snackbarMessage(message: string, action: string, duration: number): void {
    this.displayProgressSpinner = false;
    this.snackBar.open(message, action, {
      duration
    });
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

  // tslint:disable-next-line: max-line-length
  public getAppointmentStartDate(appointment: Office.AppointmentCompose, callerror: (error: any) => void, callback: (eventTime: Date) => void) {
    appointment.start.getAsync(
      result => {
        if (result.error) {
          callerror(result.error.message);
        } else {
          const mailboxDate = result.value;
          callback(mailboxDate);
        }
      }
    );
  }

  // tslint:disable-next-line: max-line-length
  public getAppointmentEndDate(appointment: Office.AppointmentCompose, callerror: (error: any) => void, callback: (eventTime: Date) => void) {
    appointment.end.getAsync(
      result => {
        if (result.error) {
          callerror(result.error.message);
        } else {
          const mailboxDate = result.value;
          callback(mailboxDate);
        }
      }
    );
  }

  // tslint:disable-next-line: max-line-length
  public getRecurrence(recurrence: Office.Recurrence, callerror: (error: any) => void, callback: (recurrenceItem: Office.Recurrence) => void) {
    recurrence.getAsync(
      result => {
        if (result.error) {
          callerror(result.error.message);
        } else {
          const recurrenceObject = result.value;
          callback(recurrenceObject);
        }
      }
    );
  }
}
