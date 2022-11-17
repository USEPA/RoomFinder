import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyCalendarComponent } from './my-calendar.component';
import { SharedTestModule } from '../../../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AuthService } from '../../../shared/services/auth-service';
import { AlertsService } from '../../../shared/services/alerts-service';
import { of } from 'rxjs';
import { ObjectMocks } from '../../../testing/mock-objects';

describe('ContactComponent', () => {
  let component: MyCalendarComponent;
  let fixture: ComponentFixture<MyCalendarComponent>;
  const mocks = new ObjectMocks();
  const mockAuthService = {
    getEvents: jest.fn()
  };

  const mockAlertsService = {
    alertsService: jest.fn()
  };

  mockAuthService.getEvents.mockReturnValue(of(mocks.getGraphEvents(4)));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        MyCalendarComponent
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: AlertsService, useValue: mockAlertsService },
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
    mockAuthService.getEvents.mockClear();
    fixture = TestBed.createComponent(MyCalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should OnInit call graph', () => {
    expect(mockAuthService.getEvents).toHaveBeenCalledTimes(1);
    expect(component.events.length).toBeGreaterThanOrEqual(4);
  });
});
