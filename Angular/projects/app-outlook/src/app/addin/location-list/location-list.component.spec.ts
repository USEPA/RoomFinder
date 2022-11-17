import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocationListComponent } from './location-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../testing/shared.test.module';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { RoomService } from '../../shared/services/room-service';
import { of } from 'rxjs';

describe('LocationListComponent', () => {
  let component: LocationListComponent;
  let fixture: ComponentFixture<LocationListComponent>;

  const mocks = new ObjectMocks();

  const mockRoomService = {
    getLocationsFromAPI: jest.fn()
  };

  const locations = mocks.getLocationsFromAPI(4);
  mockRoomService.getLocationsFromAPI.mockReturnValue(of(locations));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        LocationListComponent
      ],
      providers: [
        { provide: RoomService, useValue: mockRoomService },
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
    mockRoomService.getLocationsFromAPI.mockClear();
    fixture = TestBed.createComponent(LocationListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check locations length', () => {
    expect(mockRoomService.getLocationsFromAPI).toHaveBeenCalled();
    expect(component.locations.length).toBe(locations.length);
    expect(component.locations[0].value).toBe(locations[0].displayName);
  });

});
