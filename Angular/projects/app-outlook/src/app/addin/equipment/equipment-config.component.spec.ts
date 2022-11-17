import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentConfigComponent } from './equipment-config.component';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { of } from 'rxjs';

describe('EquipmentConfigComponent', () => {
  let component: EquipmentConfigComponent;
  let fixture: ComponentFixture<EquipmentConfigComponent>;
  const mocks = new ObjectMocks();

  const mockEquipmentService = {
    getEquipmentFromAPI: jest.fn(),
  };
  mockEquipmentService.getEquipmentFromAPI.mockReturnValue(of(mocks.getEquipments()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentConfigComponent],
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
    fixture = TestBed.createComponent(EquipmentConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should refresh equipments', () => {
    expect(component.equipmentService.getEquipmentFromAPI).toHaveBeenCalled();
  });
});
