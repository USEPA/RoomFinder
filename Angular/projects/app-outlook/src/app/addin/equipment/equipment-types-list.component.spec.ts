import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentTypesListComponent } from './equipment-types-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { of } from 'rxjs';

describe('EquipmentTypesListComponent', () => {
  let component: EquipmentTypesListComponent;
  let fixture: ComponentFixture<EquipmentTypesListComponent>;
  const mocks = new ObjectMocks();
  const mockEquipmentService = {
    getEquipmentTypesFromAPI: jest.fn()
  };
  mockEquipmentService.getEquipmentTypesFromAPI.mockReturnValue(of(mocks.getEquipmentTypes()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentTypesListComponent],
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
    fixture = TestBed.createComponent(EquipmentTypesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
