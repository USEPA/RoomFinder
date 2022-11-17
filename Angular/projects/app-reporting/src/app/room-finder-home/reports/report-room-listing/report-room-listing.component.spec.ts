import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReportRoomListingComponent } from './report-room-listing.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { ObjectMocks } from '../../../testing/mock-objects';
import { ReportingService } from '../../../shared/services/reporting-service';
import { SharedTestModule } from '../../../testing/shared.test.module';

describe('ReportRoomListingComponent', () => {
  let component: ReportRoomListingComponent;
  let fixture: ComponentFixture<ReportRoomListingComponent>;
  const mocks = new ObjectMocks();

  const mockReportingService = {
    getRoomsFromAPI: jest.fn()
  };

  const rooms = mocks.getRoomListing(4);
  mockReportingService.getRoomsFromAPI.mockReturnValue(of(rooms));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        ReportRoomListingComponent
      ],
      providers: [
        { provide: ReportingService, useValue: mockReportingService },
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
    mockReportingService.getRoomsFromAPI.mockClear();
    fixture = TestBed.createComponent(ReportRoomListingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check room length', () => {
    expect(mockReportingService.getRoomsFromAPI).toHaveBeenCalled();
    expect(component.rooms.length).toBe(rooms.length);
    expect(component.rooms[0].displayName).toBe(rooms[0].displayName);
  });
});
