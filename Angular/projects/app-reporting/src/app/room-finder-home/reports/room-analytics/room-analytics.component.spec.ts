import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomAnalyticsComponent } from './room-analytics.component';
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

describe('RoomAnalyticsComponent', () => {
  let component: RoomAnalyticsComponent;
  let fixture: ComponentFixture<RoomAnalyticsComponent>;

  const mocks = new ObjectMocks();
  const mockLoggingService = {
    logTelemetry: jest.fn(),
    logError: jest.fn(),
    logInformation: jest.fn()
  };
  const mockReportingService = {
    getReportRoomAnalyticsFromAPI: jest.fn(),
    setTimeStartDay: jest.fn(),
    setTimeEndDay: jest.fn()
  };
  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };

  const rooms = mocks.getRoomAnalytics(5);
  mockReportingService.getReportRoomAnalyticsFromAPI.mockReturnValue(of(rooms));
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
        RoomAnalyticsComponent
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
    mockReportingService.getReportRoomAnalyticsFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomAnalyticsComponent);
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

  it('should run report and load datasource', () => {
    component.startDateReport = new Date(startDate.getHours() + 1);
    component.endDateReport = new Date(endDate.getHours() + 2);
    component.runReport();
    expect(mockReportingService.getReportRoomAnalyticsFromAPI).toHaveBeenCalled();
    expect(component.dataSource.data).toBeDefined();
  });

});
