import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { RouterTestingModule } from '@angular/router/testing';
import { SharedTestModule } from './testing/shared.test.module';
import { LoggingService } from './shared/services/logging-service';
import { HomeComponent } from './room-finder-home/home/home.component';
import { RoomAnalyticsComponent } from './room-finder-home/reports/room-analytics/room-analytics.component';
import { EquipmentAnalyticsComponent } from './room-finder-home/reports/equipment-analytics/equipment-analytics.component';
import { ContactComponent } from './room-finder-home/contact/contact.component';
import { MyCalendarComponent } from './room-finder-home/reports/my-calendar/my-calendar.component';
import { RoomCalendarComponent } from './room-finder-home/reports/room-calendar/room-calendar.component';
import { MatSortModule } from '@angular/material/sort';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { AuthService } from './shared/services/auth-service';
import { AlertsService } from './shared/services/alerts-service';

describe('Reporting-AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;


  const mockLoggingService = {
    logTelemetry: jest.fn()
  };
  const mockAuthService = {
    signIn: jest.fn(),
    isAuthenticated: jest.fn(),
    userDisplayName: jest.fn()
  };
  const mockAlertService = {
    add: jest.fn(),
    get: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
        RouterTestingModule,
        MatTableModule,
        MatSortModule,
        MatDialogModule,
        MatExpansionModule,
        MatListModule,
      ],
      declarations: [
        AppComponent,
        HomeComponent,
        RoomCalendarComponent,
        RoomAnalyticsComponent,
        EquipmentAnalyticsComponent,
        ContactComponent,
        MyCalendarComponent
      ],
      providers: [
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: AlertsService, useValue: mockAlertService },
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
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });


  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it(`should have as title 'roomfinder'`, () => {
    expect(component.title).toEqual('roomfinder');
  });
});
