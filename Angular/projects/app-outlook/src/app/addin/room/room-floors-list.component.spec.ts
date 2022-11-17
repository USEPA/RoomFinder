import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomFloorsListComponent } from './room-floors-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../testing/shared.test.module';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { RoomService } from '../../shared/services/room-service';
import { of } from 'rxjs';

describe('RoomFloorsListComponent', () => {
  let component: RoomFloorsListComponent;
  let fixture: ComponentFixture<RoomFloorsListComponent>;

  const mocks = new ObjectMocks();

  const mockRoomService = {
    getFloorsFromAPI: jest.fn()
  };

  const floors = mocks.getRoomFloorsFromAPI(4);
  mockRoomService.getFloorsFromAPI.mockReturnValue(of(floors.map((element) => element.value)));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        RoomFloorsListComponent
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
    mockRoomService.getFloorsFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomFloorsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check floors length = 0', () => {
    expect(mockRoomService.getFloorsFromAPI).not.toHaveBeenCalled();
    expect(component.floors.length).toBe(0);
  });

  it('should check floors length > 0', () => {
    component.refreshFloors('ohio', 'cincinatti', 'office1');
    expect(mockRoomService.getFloorsFromAPI).toHaveBeenCalled();
    expect(component.floors.length).toBe(floors.length);
    expect(component.floors[0].value).toBe(floors[0].value);
  });
});
