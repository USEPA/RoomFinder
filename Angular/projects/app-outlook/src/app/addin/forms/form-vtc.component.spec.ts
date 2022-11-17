import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormVtcComponent } from './form-vtc.component';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { of } from 'rxjs';

describe('FormVtcComponent', () => {
  let component: FormVtcComponent;
  let fixture: ComponentFixture<FormVtcComponent>;

  const mockMatDialogRef = {
    open: jest.fn(),
    close: jest.fn()
  };

  const oldResetTestingModule = TestBed.resetTestingModule;
  beforeAll((done: any) => (async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      schemas: [NO_ERRORS_SCHEMA],
      declarations: [FormVtcComponent],
      imports: [
        MatDialogModule,
      ],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { message: null } },
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
    fixture = TestBed.createComponent(FormVtcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should default to checkboxes as populated', () => {
    expect(component.getCheckboxes().length).toBeGreaterThan(1);
  });
});
