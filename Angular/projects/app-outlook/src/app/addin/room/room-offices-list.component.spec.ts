import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomOfficesListComponent } from './room-offices-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../testing/shared.test.module';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { RoomService } from '../../shared/services/room-service';
import { of } from 'rxjs';

describe('RoomOfficesListComponent', () => {
  let component: RoomOfficesListComponent;
  let fixture: ComponentFixture<RoomOfficesListComponent>;

  const mocks = new ObjectMocks();

  const mockRoomService = {
    getOfficesFromAPI: jest.fn()
  };

  const offices = mocks.getRoomOfficesFromAPI(4);
  mockRoomService.getOfficesFromAPI.mockReturnValue(of(offices.map((element) => element.value)));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        RoomOfficesListComponent
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
    mockRoomService.getOfficesFromAPI.mockClear();
    fixture = TestBed.createComponent(RoomOfficesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should check offices length = 0', () => {
    expect(mockRoomService.getOfficesFromAPI).not.toHaveBeenCalled();
    expect(component.offices.length).toBe(0);
  });

  it('should check offices length > 0', () => {
    component.refreshOffices('ohio', 'cincinatti');
    expect(mockRoomService.getOfficesFromAPI).toHaveBeenCalled();
    expect(component.offices.length).toBe(offices.length);
    expect(component.offices[0].value).toBe(offices[0].value);
  });
});
