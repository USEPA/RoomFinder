import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentAnalyticsComponent } from './equipment-analytics.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ReportingService } from '../../../shared/services/reporting-service';
import { LoggingService } from '../../../shared/services/logging-service';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SharedTestModule } from '../../../testing/shared.test.module';
import { ObjectMocks } from '../../../testing/mock-objects';
import { of } from 'rxjs';

describe('EquipmentAnalyticsComponent', () => {
  let component: EquipmentAnalyticsComponent;
  let fixture: ComponentFixture<EquipmentAnalyticsComponent>;

  const mocks = new ObjectMocks();
  const mockLoggingService = {
    logTelemetry: jest.fn(),
    logError: jest.fn(),
    logInformation: jest.fn()
  };
  const mockReportingService = {
    getReportEquipmentAnalyticsFromAPI: jest.fn(),
    setTimeStartDay: jest.fn(),
    setTimeEndDay: jest.fn()
  };
  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };

  mockReportingService.getReportEquipmentAnalyticsFromAPI.mockReturnValue(of(mocks.getReportEquipments(5)));
  mockReportingService.setTimeStartDay.mockReturnValue(new Date('2019-10-01T01:00:00Z'));
  mockReportingService.setTimeEndDay.mockReturnValue(new Date('2019-10-01T01:30:00Z'));

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
        EquipmentAnalyticsComponent
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
    fixture = TestBed.createComponent(EquipmentAnalyticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
