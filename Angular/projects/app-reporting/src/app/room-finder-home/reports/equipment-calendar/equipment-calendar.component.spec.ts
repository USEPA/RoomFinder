import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EquipmentCalendarComponent } from './equipment-calendar.component';
import { SharedTestModule } from '../../../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('EquipmentCalendarComponent', () => {
  let component: EquipmentCalendarComponent;
  let fixture: ComponentFixture<EquipmentCalendarComponent>;


  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [EquipmentCalendarComponent]
    });
    await TestBed.compileComponents();
    TestBed.resetTestingModule = () => TestBed;
  })().then(done).catch(done.fail));
  afterAll(() => {
    TestBed.resetTestingModule = oldResetTestingModule;
    TestBed.resetTestingModule();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(EquipmentCalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
