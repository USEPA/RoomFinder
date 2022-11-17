import { SharedModule } from '../shared/shared.module';
import { HomeComponent } from './home/home.component';
import { RoomFinderHomeRoutingModule } from './room-finder-home-routing.module';
import { NgModule } from '@angular/core';
import { ContactComponent } from './contact/contact.component';
import { RoomFinderModule } from '../room-finder/room-finder.module';
import { OpenApiListComponent } from './open-api/open-api-list.component';
import { MsalModule } from '@azure/msal-angular';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule } from '@angular/material/dialog';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { ReportRoomListingComponent } from './reports/report-room-listing/report-room-listing.component';
import { EquipmentAnalyticsComponent } from './reports/equipment-analytics/equipment-analytics.component';
import { EquipmentCalendarComponent } from './reports/equipment-calendar/equipment-calendar.component';
import { RoomCalendarComponent } from './reports/room-calendar/room-calendar.component';
import { MyCalendarComponent } from './reports/my-calendar/my-calendar.component';
import { RoomAnalyticsComponent } from './reports/room-analytics/room-analytics.component';
import { RoomOverviewComponent } from './reports/room-overview/room-overview.component';
import { MatToolbarModule } from '@angular/material/toolbar';

@NgModule({
  imports: [
    SharedModule,
    RoomFinderModule,
    RoomFinderHomeRoutingModule,
    HttpClientModule,
    MatDividerModule,
    MatSnackBarModule,
    MatTableModule,
    MatSortModule,
    MatDialogModule,
    MatTabsModule,
    MatToolbarModule,
    MatExpansionModule,
    MatDatepickerModule,
    ReactiveFormsModule,
    MsalModule,
  ],
  declarations: [
    HomeComponent,
    ContactComponent,
    OpenApiListComponent,
    EquipmentAnalyticsComponent,
    EquipmentCalendarComponent,
    MyCalendarComponent,
    ReportRoomListingComponent,
    RoomAnalyticsComponent,
    RoomCalendarComponent,
    RoomOverviewComponent,
  ],
  entryComponents: [
  ],
  exports: [
  ]
})
export class RoomFinderHomeModule { }
