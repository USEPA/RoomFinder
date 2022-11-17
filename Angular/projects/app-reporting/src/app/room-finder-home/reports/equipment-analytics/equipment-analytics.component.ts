import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDatepickerInputEvent } from '@angular/material/datepicker';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EquipmentAnalyticsAPI, MeetingTimeWindowModel } from '../../../shared/models/report.model';
import { Subject, Observable, of } from 'rxjs';
import { LoggingService } from '../../../shared/services/logging-service';
import { takeUntil } from 'rxjs/operators';
import { ReportingService } from '../../../shared/services/reporting-service';

@Component({
  selector: 'app-equipment-analytics',
  templateUrl: './equipment-analytics.component.html',
  styleUrls: ['./equipment-analytics.component.scss']
})
export class EquipmentAnalyticsComponent implements OnInit, AfterViewInit {
  public currentDate = new Date();
  public startDateReport: Date = this.getFormattedDate(-1);
  public endDateReport: Date = new Date();
  public startDate = new FormControl(this.startDateReport);
  public endDate = new FormControl(this.endDateReport);
  public dateForm: FormGroup;
  public displayedColumns: string[] = ['equipmentName',
    'numberOfMeetings'];
  public dataSource = new MatTableDataSource<EquipmentAnalyticsAPI>([]);
  public equipmentAnalytics: EquipmentAnalyticsAPI[] = [];
  public equipmentAnalyticsEntity: MeetingTimeWindowModel;
  public unSubAnalyticFilter = new Subject();
  public events: Event[];
  public loading$: Observable<boolean>;

  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  // Progress Spinner Config
  public color = 'primary';
  public mode = 'indeterminate';
  public value = 50;
  public displayProgressSpinner = false;
  public spinnerWithoutBackdrop = false;

  constructor(
    public reportService: ReportingService,
    public loggingService: LoggingService,
    public snackBar: MatSnackBar
  ) { }

  ngOnInit() {
    this.loading$ = of(false);
    this.dataSource = new MatTableDataSource(this.equipmentAnalytics);
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  protected getFormattedDate(monthIncrement: number = 1) {
    return new Date(`${this.currentDate.getMonth() + monthIncrement}/${this.currentDate.getDate()}/${this.currentDate.getFullYear()}`);
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

  protected equipmentAnalyticsError(message: string, action: string, duration: number): void {
    this.displayProgressSpinner = false;
    this.snackBar.open(message, action, {
      duration
    });
  }

  runReport(): void {
    if (this.startDateReport === null) {
      this.equipmentAnalyticsError('Start Date is a required field', 'Error', 5000);
      return;
    }
    if (this.endDateReport === null) {
      this.equipmentAnalyticsError('End Date is a required field', 'Error', 5000);
      return;
    }
    this.loading$ = of(true);
    this.displayProgressSpinner = true;
    this.startDateReport = this.reportService.setTimeStartDay(this.startDateReport);
    this.endDateReport = this.reportService.setTimeEndDay(this.endDateReport);
    this.equipmentAnalyticsEntity = {
      startDate: this.startDateReport,
      endDate: this.endDateReport
    };
    console.log(`equipmentAnalyticsEntity: ${this.equipmentAnalyticsEntity}`);
    this.reportService
      .getReportEquipmentAnalyticsFromAPI(this.equipmentAnalyticsEntity)
      .pipe(takeUntil(this.unSubAnalyticFilter))
      .subscribe(
        analytics => {
          this.dataSource = new MatTableDataSource<EquipmentAnalyticsAPI>(analytics);
          this.dataSource.sort = this.sort;
          this.dataSource.paginator = this.paginator;
          this.loading$ = of(false);
          this.displayProgressSpinner = false;
        },
        err => {
          this.equipmentAnalyticsError(err.message, err.status, 5000);
          this.loggingService.logError(`${err.message}, ${err.status}`, ['runReport-EWS Error-HTTP Error']);
        },
        () => {
          this.displayProgressSpinner = false;
          this.loggingService.logInformation('HTTP request completed.', ['runReport-getReportEquipmentAnalyticsFromAPI']);
        }
      );
  }
}
