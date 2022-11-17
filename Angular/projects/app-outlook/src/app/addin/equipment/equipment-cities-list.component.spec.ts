import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentCitiesListComponent } from './equipment-cities-list.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { of } from 'rxjs';

describe('EquipmentCitiesListComponent', () => {
  let component: EquipmentCitiesListComponent;
  let fixture: ComponentFixture<EquipmentCitiesListComponent>;
  const mocks = new ObjectMocks();

  const mockEquipmentService = {
    getCitiesFromAPI: jest.fn(),
  };
  mockEquipmentService.getCitiesFromAPI.mockReturnValue(of(mocks.getEquipmentCities()));

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [EquipmentCitiesListComponent],
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
    fixture = TestBed.createComponent(EquipmentCitiesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should refresh cities', () => {
    component.refreshCities('Cincinatti');
    expect(component.citiesListService.getCitiesFromAPI).toHaveBeenCalledTimes(1);
  });
});
