import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomOverviewComponent } from './room-overview.component';
import { SharedTestModule } from '../../../testing/shared.test.module';
import { NO_ERRORS_SCHEMA } from '@angular/core';

describe('RoomOverviewComponent', () => {
  let component: RoomOverviewComponent;
  let fixture: ComponentFixture<RoomOverviewComponent>;

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        RoomOverviewComponent
      ],
      providers: [
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
    fixture = TestBed.createComponent(RoomOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
