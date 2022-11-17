import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomCalendarComponent } from './room-calendar.component';
import { ReportingService } from '../../../shared/services/reporting-service';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../../testing/shared.test.module';
import { ObjectMocks } from '../../../testing/mock-objects';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LoggingService } from '../../../shared/services/logging-service';
import { of } from 'rxjs';

describe('RoomCalendarComponent', () => {
  let component: RoomCalendarComponent;
  let fixture: ComponentFixture<RoomCalendarComponent>;

  const mocks = new ObjectMocks();
  const mockLoggingService = {
    logTelemetry: jest.fn(),
    logError: jest.fn(),
    logInformation: jest.fn()
  };
  const mockReportingService = {
    getReportRoomEventsFromAPI: jest.fn(),
    setTimeStartDay: jest.fn(),
    setTimeEndDay: jest.fn()
  };
  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };

  const rooms = mocks.getReportRoomEvents(5);
  mockReportingService.getReportRoomEventsFromAPI.mockReturnValue(of(rooms));
  const startDate = new Date('2019-10-01T01:00:00Z');
  const endDate = new Date('2019-10-01T01:30:00Z');
  mockReportingService.setTimeStartDay.mockReturnValue(startDate);
  mockReportingService.setTimeEndDay.mockReturnValue(endDate);

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
        MatTableModule,
        MatSortModule,
        MatDialogModule,
      ],
      declarations: [
        RoomCalendarComponent
      ],
      providers: [
        MatPaginatorIntl,
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: ReportingService, useValue: mockReportingService },
        { provide: MatSnackBar, useValue: mockMatSnackbar },
      ],
    });
    await TestBed.compileComponents();
    TestBed.resetTestingModule = () => TestBed;
  })().then(done).catch(done.fail));
  afterAll(() => {
    TestBed.resetTestingModule = oldResetTestingModule;
    TestBed.resetTestingModule();
  });

  beforeEach(() => {
    mockReportingService.getReportRoomEventsFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomCalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should OnInit datasource initialized', () => {
    expect(component.dataSource).toBeDefined();
    expect(component.dataSource.data.length).toBe(0);
  });

  it('should run report fail no room set', () => {
    const reportStartDate = new Date();
    reportStartDate.setHours(reportStartDate.getHours() + 1);
    component.startDateReport = reportStartDate;
    const reportEndDate = new Date();
    reportEndDate.setHours(reportEndDate.getHours() + 2);
    component.endDateReport = reportEndDate;
    component.runReport();
    expect(mockReportingService.getReportRoomEventsFromAPI).not.toHaveBeenCalled();
  });

  it('should run report retreive data source', () => {
    const reportStartDate = new Date();
    reportStartDate.setHours(reportStartDate.getHours() + 1);
    component.startDateReport = reportStartDate;
    const reportEndDate = new Date();
    reportEndDate.setHours(reportEndDate.getHours() + 2);
    component.endDateReport = reportEndDate;
    component.selectRoom = 'room1';
    component.runReport();
    expect(mockReportingService.getReportRoomEventsFromAPI).toHaveBeenCalled();
    expect(component.dataSource.data).toBeDefined();
    expect(component.dataSource.data.length).toBeGreaterThanOrEqual(rooms.length);
  });

});
