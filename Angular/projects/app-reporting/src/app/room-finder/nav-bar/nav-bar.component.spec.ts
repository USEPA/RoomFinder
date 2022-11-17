import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NavBarComponent } from './nav-bar.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AuthService } from '../../shared/services/auth-service';
import { ObjectMocks } from '../../testing/mock-objects';
import { RouterTestingModule } from '@angular/router/testing';
import { SharedTestModule } from '../../testing/shared.test.module';

describe('NavBarComponent', () => {
  let component: NavBarComponent;
  let fixture: ComponentFixture<NavBarComponent>;

  const mockObject = new ObjectMocks();
  const mockService = {
    checkoutAccount: jest.fn(),
    signIn: jest.fn(),
    signOut: jest.fn(),
    userDisplayName: jest.fn(),
    isAuthenticated: jest.fn()
  };
  mockService.userDisplayName.mockReturnValue(mockObject.getUser().displayName);
  mockService.isAuthenticated.mockReturnValue(false);

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [NavBarComponent],
      imports: [
        SharedTestModule,
        RouterTestingModule,
      ],
      providers: [
        { provide: AuthService, useValue: mockService }
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
    fixture = TestBed.createComponent(NavBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
