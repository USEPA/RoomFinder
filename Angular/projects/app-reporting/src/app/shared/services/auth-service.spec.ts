import { TestBed, getTestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth-service';
import { AlertsService } from './alerts-service';
import { of } from 'rxjs';
import { ObjectMocks } from '../../testing/mock-objects';
import { AppConfigService } from './app.config.service';

describe('AuthService', () => {
  let injector: TestBed;
  let service: AuthService;
  let httpMock: HttpTestingController;
  const mocker = new ObjectMocks();

  const mockAuthService = {
    getAccessToken: jest.fn(),
    userDisplayName: jest.fn(),
    loginRedirect: jest.fn(),
    getAccount: jest.fn(),
  };
  mockAuthService.getAccessToken.mockReturnValue(of('bearertoken'));
  mockAuthService.userDisplayName.mockReturnValue(mocker.getUser().displayName);
  mockAuthService.getAccount.mockReturnValue(of(mocker.getAuthAccount()));
  mockAuthService.loginRedirect.mockImplementation(() => { });
  const mockAlertService = {
    add: jest.fn()
  };
  mockAlertService.add.mockImplementation((message: string, debug: string) => {
    console.log(`${message} header with statement:${debug}`);
  });
  const mockAppConfigService = {
    getConfig: mocker.getAppConfig()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule
      ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: AlertsService, useValue: mockAlertService },
        { provide: AppConfigService, useValue: mockAppConfigService },
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
    service = injector.inject(AuthService);
    httpMock = injector.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

});
