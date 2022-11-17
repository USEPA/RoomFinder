import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddInComponent } from './addin.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { LoggingService } from '../shared/services/logging-service';
import { of } from 'rxjs';

describe('AddinComponent', () => {
  let component: AddInComponent;
  let fixture: ComponentFixture<AddInComponent>;

  const mockLoggingService = {
    logInformation: jest.fn(),
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {

    spyOn(document, 'getElementById').and.callFake(() => {
      return {
        style: {
          display: 'inline'
        }
      };
    });

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [AddInComponent],
      imports: [
      ],
      providers: [
        { provide: LoggingService, useValue: mockLoggingService },
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
    fixture = TestBed.createComponent(AddInComponent);
    component = fixture.componentInstance;
    const onLoadOffice = jest.spyOn(component, 'onLoadOfficeAddIn');
    onLoadOffice.mockImplementation(() => {
      component.loadingOfficeAddIn$ = of(true);
      component.loggingService.logInformation('hello', []);
    });
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have called logging service', () => {
    expect(mockLoggingService.logInformation).toHaveBeenCalled();
  });
});
