import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AlertsComponent } from './alerts.component';
import { AlertsService } from '../../shared/services/alerts-service';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Alert } from '../../shared/models/alert.model';
import { ObjectMocks } from '../../testing/mock-objects';

describe('AlertsComponent', () => {
  let component: AlertsComponent;
  let fixture: ComponentFixture<AlertsComponent>;
  const mocks = new ObjectMocks();

  const mockAlertService = {
    remove: jest.fn(),
    add: jest.fn(),
    get: jest.fn()
  };
  mockAlertService.add.mockReturnValue(1);
  mockAlertService.get.mockReturnValue(mocks.getAlerts());

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [AlertsComponent],
      imports: [
      ],
      providers: [
        { provide: AlertsService, useValue: mockAlertService }
      ]
    });
    await TestBed.compileComponents();
    TestBed.resetTestingModule = () => TestBed;
  })().then(done).catch(done.fail));
  afterAll(() => {
    TestBed.resetTestingModule = oldResetTestingModule;
    TestBed.resetTestingModule();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AlertsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should pop alert off stack', () => {
    const alert: Alert = {
      message: 'message',
      debug: 'debug'
    };
    component.close(alert);
    expect(component.alertsService.remove).toHaveBeenCalled();
  });
});
