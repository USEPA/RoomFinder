import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomEventsAPI, RoomEventsEntity } from '../../../shared/models/report.model';
import { LoggingService } from '../../../shared/services/logging-service';
import { takeUntil } from 'rxjs/operators';
import { Subject, Observable, of } from 'rxjs';
import { ReportRoomListingComponent } from '../report-room-listing/report-room-listing.component';
import { FormGroup, FormControl } from '@angular/forms';
import { ReportingService } from '../../../shared/services/reporting-service';

@Component({
  selector: 'app-room-calendar',
  templateUrl: './room-calendar.component.html',
  styleUrls: ['./room-calendar.component.scss']
})
export class RoomCalendarComponent implements OnInit, AfterViewInit {
  public selectRoom = '';
  public currentDate = new Date();
  public startDateReport: Date = new Date(`${this.currentDate.getMonth() + 1}/01/${this.currentDate.getFullYear()}`);
  public endDateReport: Date = this.getFormattedDate(1);
  public startDate = new FormControl(this.startDateReport);
  public endDate = new FormControl(this.endDateReport);
  public dateForm: FormGroup;
  public displayedColumns: string[] = ['location',
    'organizerName',
    'subject',
    'startTimeString',
    'endTimeString',
    'isOnlineMeeting',
    'isAllDayEvent',
    'isRecurring',
    'isCancelled',
    'timeZone'];
  public dataSource: MatTableDataSource<RoomEventsAPI>;
  public roomEvents: RoomEventsAPI[] = [];
  public roomEventsEntity: RoomEventsEntity;
  public unSubEventFilter = new Subject();
  public events: Event[];
  public loading$: Observable<boolean>;

  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild('roomsList') roomsList: ReportRoomListingComponent;

  // Progress Spinner Config
  public color = 'primary';
  public mode = 'indeterminate';
  public value = 50;
  public displayProgressSpinner = false;
  public spinnerWithoutBackdrop = false;

  constructor(
    public reportService: ReportingService,
    public loggingService: LoggingService,
    public snackBar: MatSnackBar,
  ) {
  }

  ngOnInit() {
    this.loading$ = of(false);
    this.dataSource = new MatTableDataSource<RoomEventsAPI>([]);
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  protected getFormattedDate(monthIncrement: number = 1) {
    return new Date(`${this.currentDate.getMonth() + monthIncrement}/${this.currentDate.getDate()}/${this.currentDate.getFullYear()}`);
  }

  checkRooms(selectedRoom: any): void {
    this.selectRoom = selectedRoom;
    console.log(`Selected Room: ${this.selectRoom}`);
  }

  startDateChange(event: MatDatepickerInputEvent<Date>): void {
    this.startDateReport = event.value;
  }

  endDateChange(event: MatDatepickerInputEvent<Date>): void {
    this.endDateReport = event.value;
  }

  applyFilter(filterValue: string): void {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.dataSource.filter = filterValue;

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  protected roomEventsError(message: string, action: string, duration: number): void {
    this.displayProgressSpinner = false;
    this.snackBar.open(message, action, {
      duration
    });
  }

  runReport(): void {
    if (this.startDateReport === null) {
      this.roomEventsError('Start Date is a required field', 'Error', 5000);
      return;
    }
    if (this.endDateReport === null) {
      this.roomEventsError('End Date is a required field', 'Error', 5000);
      return;
    }
    if (this.selectRoom === '') {
      this.roomEventsError('Select a Room is a required field', 'Error', 5000);
      return;
    }
    this.loading$ = of(true);
    this.displayProgressSpinner = true;
    this.startDateReport = this.reportService.setTimeStartDay(this.startDateReport);
    this.endDateReport = this.reportService.setTimeEndDay(this.endDateReport);

    this.roomEventsEntity = new RoomEventsEntity();
    this.roomEventsEntity.startDate = this.startDateReport;
    this.roomEventsEntity.endDate = this.endDateReport;
    this.roomEventsEntity.emailAddress = this.selectRoom;

    this.reportService
      .getReportRoomEventsFromAPI(this.roomEventsEntity)
      .pipe(takeUntil(this.unSubEventFilter))
      .subscribe(
        events => {
          this.dataSource = new MatTableDataSource<RoomEventsAPI>(events);
          this.dataSource.sort = this.sort;
          this.dataSource.paginator = this.paginator;
          this.loading$ = of(false);
          this.displayProgressSpinner = false;
        },
        err => {
          this.roomEventsError(err.message, err.status, 5000);
          this.loggingService.logError(`${err.message}, ${err.status}`, ['runReport-EWS Error-HTTP Error']);
        },
        () => {
          this.displayProgressSpinner = false;
          this.loggingService.logInformation('HTTP request completed.', ['runReport-getReportRoomEventsFromAPI']);
        }
      );
  }
}
