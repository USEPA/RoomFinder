import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormAvailabilityRequestComponent } from './form-availabilityrequest.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatTabsModule } from '@angular/material/tabs';
import { of } from 'rxjs';
import { ObjectMocks } from '../../testing/addin-mock-objects';
import { SharedTestModule } from '../../testing/shared.test.module';
import { By } from '@angular/platform-browser';

describe('AvailabilityrequestComponent', () => {
  let component: FormAvailabilityRequestComponent;
  let fixture: ComponentFixture<FormAvailabilityRequestComponent>;
  const mocks = new ObjectMocks();

  const mockMatDialogRef = {
    open: jest.fn(),
    close: jest.fn()
  };

  const mockMatDialogData = mocks.getMatDialogData(25);

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [FormAvailabilityRequestComponent],
      imports: [
        MatDialogModule,
        MatTableModule,
        MatTabsModule,
        MatSortModule,
        SharedTestModule,
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: mockMatDialogData },
        { provide: MatDialogRef, useValue: mockMatDialogRef }
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
    mockMatDialogRef.open.mockImplementation(() => ({ afterClosed: () => of(true) }));
    mockMatDialogRef.close.mockClear();
    fixture = TestBed.createComponent(FormAvailabilityRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should evalate recurring availability', () => {
    fixture.detectChanges();
    const htmlElement = fixture.debugElement.query(By.css('.timeStart'));
    expect(htmlElement).toBeTruthy();
    expect(htmlElement.nativeElement.textContent).toContain('2019-10-02T01:00:00Z');
  });

  it('should populate attendees for recurring check', () => {
    expect(component.dataSource.data).toBeDefined();
    expect(component.dataSource.data.length).toBeGreaterThanOrEqual(25);
  });

  it('should no click and close dialog', () => {
    component.onNoClick();
    expect(mockMatDialogRef.close).toHaveBeenCalled();
  });
});
