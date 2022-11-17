import { NgModule } from '@angular/core';
import { MatOptionModule } from '@angular/material/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { environment } from '../../environments/environment';
import { NgbAlertModule, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { OverlayModule } from '@angular/cdk/overlay';
import { ZuluDateFormatPipe } from './pipes/zulu-date-format.pipe';
import { DateFormatPipe } from './pipes/date-format.pipe';

@NgModule({
  imports: [
    CommonModule,
    FlexLayoutModule,
    FormsModule,
    FontAwesomeModule,
    OverlayModule,
    ReactiveFormsModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatExpansionModule,
    MatSnackBarModule,
    MatInputModule,
    MatSelectModule,
    MatOptionModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatMenuModule,
    MatTableModule,
    MatTabsModule,
    MatIconModule,
    RouterModule,
    NgbAlertModule,
    NgbModule,
  ],
  declarations: [
    DateFormatPipe,
    ZuluDateFormatPipe,
  ],
  entryComponents: [],
  providers: [
    DatePipe,
    DateFormatPipe,
    ZuluDateFormatPipe,
  ],
  exports: [
    CommonModule,
    FlexLayoutModule,
    FontAwesomeModule,
    RouterModule,
    FormsModule,
    OverlayModule,
    ReactiveFormsModule,
    MatCheckboxModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatExpansionModule,
    MatSnackBarModule,
    MatInputModule,
    MatSelectModule,
    MatOptionModule,
    MatIconModule,
    MatPaginatorModule,
    MatMenuModule,
    MatTableModule,
    MatTabsModule,
    MatIconModule,
    NgbAlertModule,
    DatePipe,
    DateFormatPipe,
    ZuluDateFormatPipe,
  ],
  bootstrap: [
  ]
})
export class SharedModule { }

export function DisableAnimation(): boolean {
  return environment.disableAnimation;
}
