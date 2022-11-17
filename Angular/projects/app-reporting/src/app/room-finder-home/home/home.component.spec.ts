import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { By } from '@angular/platform-browser';
import { LoggingService } from '../../shared/services/logging-service';
import { AuthService } from '../../shared/services/auth-service';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  const mockLoggingService = {
    logTelemetry: jest.fn()
  };
  const mockAuthService = {
    signIn: jest.fn(),
    isAuthenticated: jest.fn(),
    userDisplayName: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [],
      declarations: [
        HomeComponent
      ],
      providers: [
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: AuthService, useValue: mockAuthService },
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
    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render title', () => {
    fixture.detectChanges();
    const htmlElement = fixture.debugElement.query(By.css('.lead'));
    expect(htmlElement).toBeTruthy();
    expect(htmlElement.nativeElement.textContent).toContain('provide insight into conference rooms');
  });
});
