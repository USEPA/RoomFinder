import { TestBed, inject, getTestBed } from '@angular/core/testing';
import { HttpTestingController, HttpClientTestingModule } from '@angular/common/http/testing';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { AppConfigService } from './app.config.service';
import { EquipmentService } from './equipment-service';
import { LoggingService } from './logging-service';

describe('EquipmentService', () => {
  let injector: TestBed;
  let service: EquipmentService;
  let httpMock: HttpTestingController;
  const mocker = new ObjectMocks();
  const mockAppConfigService = {
    getConfig: jest.fn()
  };
  const mockLogService = {

  };
  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };


  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        MatSnackBarModule
      ],
      providers: [
        EquipmentService,
        { provide: AppConfigService, useValue: mockAppConfigService },
        { provide: MatSnackBar, useValue: mockMatSnackbar },
        { provide: LoggingService, useValue: mockLogService },
      ]
    });
    await TestBed.compileComponents();
    TestBed.resetTestingModule = () => TestBed;

    mockAppConfigService.getConfig.mockReturnValue(mocker.getAppConfig());
  })().then(done).catch(done.fail));
  afterAll(() => {
    TestBed.resetTestingModule = oldResetTestingModule;
    TestBed.resetTestingModule();
  });

  beforeEach(() => {
    injector = getTestBed();
    service = injector.inject(EquipmentService);
    httpMock = injector.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', inject([EquipmentService], () => {
    expect(service).toBeTruthy();
  }));

  describe('getFilterResults', () => {
    it('should return an Array<Equipment>', inject([EquipmentService], () => {
      const equipments = mocker.getEquipmentFilter(2);
      const equipmentFilter = mocker.getResourceFilter('2020-10-01T01:30:00Z', '2020-10-02T01:30:00Z', true);

      service.postEquipmentFind(equipmentFilter).subscribe(filteredResult => {
        expect(filteredResult.length).toBe(equipments.length);
      });
      const url = mocker.getAppConfig().auth.baseWebApiUrl;
      const req = httpMock.expectOne(`${url}api/equipmentDataService/v1.0/find/`);
      expect(req.request.method).toBe('POST');
      req.flush(equipments);
    }));
  });

});
