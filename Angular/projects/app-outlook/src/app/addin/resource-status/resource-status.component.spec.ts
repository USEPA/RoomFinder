import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResourceStatusComponent } from './resource-status.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SharedTestModule } from '../../testing/shared.test.module';
import { ResourceStatusFilterService } from '../../shared/services/resource-status-filter-service';

describe('ResourceStatusComponent', () => {
  let component: ResourceStatusComponent;
  let fixture: ComponentFixture<ResourceStatusComponent>;

  const mockResourceStatusFilterService = {
    getResourceStatusFilter: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      imports: [
        SharedTestModule,
      ],
      declarations: [
        ResourceStatusComponent
      ],
      providers: [
        { provide: ResourceStatusFilterService, useValue: mockResourceStatusFilterService },
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
    fixture = TestBed.createComponent(ResourceStatusComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
