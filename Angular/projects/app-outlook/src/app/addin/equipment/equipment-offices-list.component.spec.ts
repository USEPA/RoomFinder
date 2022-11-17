import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentOfficesListComponent } from './equipment-offices-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { of } from 'rxjs';

describe('EquipmentOfficesListComponent', () => {
  let component: EquipmentOfficesListComponent;
  let fixture: ComponentFixture<EquipmentOfficesListComponent>;
  const mocks = new ObjectMocks();
  const mockEquipmentService = {
    getOfficesFromAPI: jest.fn()
  };
  mockEquipmentService.getOfficesFromAPI.mockReturnValue(of(mocks.getEquipmentOffices()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentOfficesListComponent],
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
    fixture = TestBed.createComponent(EquipmentOfficesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call underlying service', () => {
    component.refreshOffices('OH', 'Cincinatti');
    expect(component.equipmentService.getOfficesFromAPI).toHaveBeenCalledTimes(1);
  });

  it('should call underlying service', () => {
    component.refreshOffices('OH', 'Cincinatti');
    expect(component.offices.length).toBe(5);
  });
});
