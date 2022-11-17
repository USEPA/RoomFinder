import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { SharedTestModule } from './testing/shared.test.module';
import { LoggingService } from './shared/services/logging-service';
import { AddInComponent } from './addin/addin.component';
import { MatSortModule } from '@angular/material/sort';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { AlertsService } from './shared/services/alerts-service';


describe('Outlook-AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;

  const mockLoggingService = {
    logTelemetry: jest.fn()
  };
  const mockAlertService = {
    add: jest.fn(),
    get: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {


    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
        MatTableModule,
        MatSortModule,
        MatDialogModule,
        MatExpansionModule,
        MatListModule,
      ],
      declarations: [
        AppComponent,
        AddInComponent,
      ],
      providers: [
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: AlertsService, useValue: mockAlertService },
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
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });


  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it(`should have as title 'roomfinder'`, () => {
    expect(component.title).toEqual('roomfinder');
  });
});
