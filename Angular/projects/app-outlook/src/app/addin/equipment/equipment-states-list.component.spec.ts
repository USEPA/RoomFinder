import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentStatesListComponent } from './equipment-states-list.component';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { of } from 'rxjs';

describe('EquipmentStatesListComponent', () => {
  let component: EquipmentStatesListComponent;
  let fixture: ComponentFixture<EquipmentStatesListComponent>;
  const mocks = new ObjectMocks();
  const mockEquipmentService = {
    getStatesFromAPI: jest.fn()
  };
  mockEquipmentService.getStatesFromAPI.mockReturnValue(of(mocks.getEquipmentStates()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentStatesListComponent],
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
    mockEquipmentService.getStatesFromAPI.mockClear();
    fixture = TestBed.createComponent(EquipmentStatesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call service onInit', () => {
    expect(component.equipmentService.getStatesFromAPI).toHaveBeenCalledTimes(1);
  });
});
