import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentFloorsListComponent } from './equipment-floors-list.component';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { of } from 'rxjs';

describe('EquipmentFloorsListComponent', () => {
  let component: EquipmentFloorsListComponent;
  let fixture: ComponentFixture<EquipmentFloorsListComponent>;
  const mocks = new ObjectMocks();

  const mockEquipmentService = {
    getFloorsFromAPI: jest.fn(),
  };
  mockEquipmentService.getFloorsFromAPI.mockReturnValue(of(mocks.getEquipmentFloors()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentFloorsListComponent],
      imports: [
      ],
      providers: [
        { provide: EquipmentService, useValue: mockEquipmentService },
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
    fixture = TestBed.createComponent(EquipmentFloorsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should refresh floors', () => {
    component.refreshFloors('Ohio', 'Cincinatti', 'jestHouse');
    expect(component.equipmentService.getFloorsFromAPI).toHaveBeenCalledTimes(1);
  });
});
