import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ObjectMocks } from '../../testing/mock-objects';
import { RouterTestingModule } from '@angular/router/testing';
import { SharedTestModule } from '../../testing/shared.test.module';
import { NavFooterComponent } from './nav-footer.component';
import { AppConfigService } from '../../shared/services/app.config.service';

describe('NavFooterComponent', () => {
  let component: NavFooterComponent;
  let fixture: ComponentFixture<NavFooterComponent>;

  const mockObject = new ObjectMocks();
  const mockService = {
    getVersion: jest.fn(),
    getConfig: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {

    mockService.getConfig.mockReturnValue(mockObject.getAppConfig());
    mockService.getVersion.mockReturnValue(mockObject.getAppConfig().env.version);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [NavFooterComponent],
      imports: [
        SharedTestModule,
        RouterTestingModule,
      ],
      providers: [
        { provide: AppConfigService, useValue: mockService }
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
    fixture = TestBed.createComponent(NavFooterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call get version', () => {
    expect(component).toBeTruthy();
    expect(mockService.getVersion).toHaveBeenCalled();
  });
});
