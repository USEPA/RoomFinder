import { TestBed, inject, getTestBed } from '@angular/core/testing';
import { AlertsService } from './alerts-service';

describe('AlertsService', () => {
  let injector: TestBed;
  let service: AlertsService;

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [],
      providers: [
        AlertsService,
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
    injector = getTestBed();
    service = injector.inject(AlertsService);
    service.alerts = [];
  });

  afterEach(() => {
  });

  it('should be created', inject([AlertsService], () => {
    expect(service).toBeTruthy();
  }));

  it('should accept add message and push to alert array', () => {
    service.add('hello world', 'debug');
    expect(service.alerts.length).toBeGreaterThan(0);
  });

  it('should removed added message and pop from alert array', () => {
    const alertLength = service.alerts.length;
    service.add('hello world', 'debug');
    expect(service.alerts.length).toBeGreaterThan(alertLength);
    service.remove({ message: 'hello world', debug: 'debug' });
    expect(service.alerts.length).toBe(alertLength);
  });

});
