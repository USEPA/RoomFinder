import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomStatesListComponent } from './room-states-list.component';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { RoomService } from '../../shared/services/room-service';
import { SharedTestModule } from '../../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';

describe('RoomStatesListComponent', () => {
  let component: RoomStatesListComponent;
  let fixture: ComponentFixture<RoomStatesListComponent>;

  const mocks = new ObjectMocks();

  const mockRoomService = {
    getStatesFromAPI: jest.fn()
  };

  const states = mocks.getRoomStatesFromAPI(4);
  mockRoomService.getStatesFromAPI.mockReturnValue(of(states.map((element) => element.value)));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        RoomStatesListComponent
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
    mockRoomService.getStatesFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomStatesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check states length', () => {
    expect(mockRoomService.getStatesFromAPI).toHaveBeenCalled();
    expect(component.states.length).toBe(states.length);
    expect(component.states[0].value).toBe(states[0].value);
  });
});
