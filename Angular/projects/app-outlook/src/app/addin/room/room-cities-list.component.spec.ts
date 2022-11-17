import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomCitiesListComponent } from './room-cities-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../testing/shared.test.module';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { RoomService } from '../../shared/services/room-service';
import { of } from 'rxjs';

describe('RoomCitiesListComponent', () => {
  let component: RoomCitiesListComponent;
  let fixture: ComponentFixture<RoomCitiesListComponent>;

  const mocks = new ObjectMocks();

  const mockRoomService = {
    getCitiesFromAPI: jest.fn()
  };

  const cities = mocks.getRoomFloorsFromAPI(4);
  mockRoomService.getCitiesFromAPI.mockReturnValue(of(cities.map((element) => element.value)));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        RoomCitiesListComponent
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
    mockRoomService.getCitiesFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomCitiesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check cities length = 0', () => {
    expect(mockRoomService.getCitiesFromAPI).not.toHaveBeenCalled();
    expect(component.cities.length).toBe(0);
  });

  it('should check cities length > 0', () => {
    component.refreshCities('ohio');
    expect(mockRoomService.getCitiesFromAPI).toHaveBeenCalled();
    expect(component.cities.length).toBe(cities.length);
    expect(component.cities[0].value).toBe(cities[0].value);
  });
});
