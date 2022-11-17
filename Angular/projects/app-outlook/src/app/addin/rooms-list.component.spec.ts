import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomsListComponent } from './rooms-list.component';
import { ObjectMocks } from '../testing/addin-mock-objects';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { MatMenuModule } from '@angular/material/menu';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { SharedTestModule } from '../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { LoggingService } from '../shared/services/logging-service';
import { RoomService } from '../shared/services/room-service';
import { ResourceStatusFilterService } from '../shared/services/resource-status-filter-service';


describe('RoomsListComponent', () => {
  let component: RoomsListComponent;
  let fixture: ComponentFixture<RoomsListComponent>;

  const mocks = new ObjectMocks();
  const mockLoggingService = {
    logTelemetry: jest.fn(),
    logError: jest.fn(),
    logInformation: jest.fn(),
    logItemAddInInfo: jest.fn()
  };
  const mockRoomService = {
    getRoomFilterFromAPI: jest.fn()
  };
  const mockResourceStatusService = jest.fn();
  const includeUnavailable = true;
  const includeRestricted = true;
  mockResourceStatusService.bind(includeUnavailable);
  mockResourceStatusService.bind(includeRestricted);

  const mockMatSnackbar = {
    open: jest.fn(),
    dismiss: jest.fn()
  };
  const mockMatDialog = {
    open: jest.fn(),
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {

    const rooms = mocks.getRoomFilterResults(5);
    mockRoomService.getRoomFilterFromAPI.mockReturnValue(of(rooms));
    mockMatDialog.open.mockImplementation(() => ({ afterClosed: () => of(true) }));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [
        RoomsListComponent
      ],
      imports: [
        SharedTestModule,
        MatDialogModule,
        MatMenuModule,
        MatExpansionModule,
        MatListModule,
        MatTableModule,
      ],
      providers: [
        MatPaginatorIntl,
        { provide: MAT_DIALOG_DATA, useValue: {} },
        { provide: MatDialog, useValue: mockMatDialog },
        { provide: MatSnackBar, useValue: mockMatSnackbar },
        { provide: LoggingService, useValue: mockLoggingService },
        { provide: RoomService, useValue: mockRoomService },
        { provide: ResourceStatusFilterService, useValue: mockResourceStatusService },
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
    mockRoomService.getRoomFilterFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should OnInit datasource initialized', () => {
    expect(component.dataSource).toBeDefined();
    expect(component.dataSource.data.length).toBe(0);
  });

});
