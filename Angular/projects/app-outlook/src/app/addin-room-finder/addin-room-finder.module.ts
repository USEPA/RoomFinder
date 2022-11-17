import { NgModule } from '@angular/core';
import { AlertsComponent } from './alerts/alerts.component';
import { ProgressSpinnerComponent } from './progress-spinner/progress-spinner.component';
import { SharedModule } from '../shared/shared.module';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';

@NgModule({
  imports: [
    CommonModule,
    SharedModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
    MatMenuModule,
    MatIconModule,
  ],
  declarations: [
    AlertsComponent,
    ProgressSpinnerComponent,
  ],
  entryComponents: [
  ],
  exports: [
    AlertsComponent,
    ProgressSpinnerComponent,
  ],
  bootstrap: [
    AlertsComponent,
    ProgressSpinnerComponent,
  ]
})
export class AddInRoomFinderModule { }
