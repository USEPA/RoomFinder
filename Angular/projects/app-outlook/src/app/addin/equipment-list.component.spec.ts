import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentListComponent } from './equipment-list.component';
import { ObjectMocks } from '../testing/addin-mock-objects';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../shared/services/equipment-service';
import { of } from 'rxjs';
import { ResourceStatusFilterService } from '../shared/services/resource-status-filter-service';
import { LoggingService } from '../shared/services/logging-service';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';

describe('EquipmentListComponent', () => {
  let component: EquipmentListComponent;
  let fixture: ComponentFixture<EquipmentListComponent>;
  const mocks = new ObjectMocks();

  const mockEquipmentService = {
    getFloorsFromAPI: jest.fn(),
  };
  const mockResourceStatusService = jest.fn();
  const includeUnavailable = true;
  const includeRestricted = true;
  mockResourceStatusService.bind(includeUnavailable);
  mockResourceStatusService.bind(includeRestricted);

  const mockLoggingService = {
    logItemAddInError: jest.fn(),
    logItemAddInInfo: jest.fn(),
  };
  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };

  mockEquipmentService.getFloorsFromAPI.mockReturnValue(of(mocks.getEquipmentFloors()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentListComponent],
      imports: [
        MatTableModule,
        MatSnackBarModule,
      ],
      providers: [
        { provide: EquipmentService, useValue: mockEquipmentService },
        { provide: ResourceStatusFilterService, useValue: mockResourceStatusService },
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: MatSnackBar, useValue: mockMatSnackbar },
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
    fixture = TestBed.createComponent(EquipmentListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

});
