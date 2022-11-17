import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProgressSpinnerComponent } from './progress-spinner.component';
import { OverlayService } from '../../shared/services/overlay-service';
import { SharedTestModule } from '../../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { LoggingService } from '../../shared/services/logging-service';
import { GlobalPositionStrategy } from '@angular/cdk/overlay';

describe('ProgressSpinnerComponent', () => {
  let component: ProgressSpinnerComponent;
  let fixture: ComponentFixture<ProgressSpinnerComponent>;
  let overlayService: OverlayService;

  const mockOverlayService = {
    positionGloballyCenter: jest.fn(),
    createOverlay: jest.fn(),
    attachTemplatePortal: jest.fn()
  };
  const mockLoggingService = {
    logInformation: jest.fn()
  };
  const mockOverlayRef = {
    hasAttached: jest.fn(),
    detach: jest.fn()
  };

  mockOverlayRef.hasAttached.mockReturnValue(true);
  mockOverlayRef.detach.mockImplementation(() => { });
  mockOverlayService.positionGloballyCenter.mockReturnValue(new GlobalPositionStrategy());
  mockOverlayService.attachTemplatePortal.mockImplementation(() => { });
  mockOverlayService.createOverlay.mockReturnValue(mockOverlayRef);

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        ProgressSpinnerComponent
      ],
      providers: [
        { provide: OverlayService, useValue: mockOverlayService },
        { provide: LoggingService, useValue: mockLoggingService },
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
    fixture = TestBed.createComponent(ProgressSpinnerComponent);
    component = fixture.componentInstance;
    overlayService = TestBed.inject(OverlayService);
  });

  it('should create', () => {
    const ngDoCheckMethod = jest.spyOn(component, 'ngDoCheck');
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(overlayService.createOverlay).toHaveBeenCalled();
    expect(ngDoCheckMethod).toHaveBeenCalled();
  });

});
