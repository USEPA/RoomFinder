import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { NgModule } from '@angular/core';
import { ContactComponent } from './contact/contact.component';
import { MsalGuard } from '@azure/msal-angular';
import { OpenApiListComponent } from './open-api/open-api-list.component';
import { RoomCalendarComponent } from './reports/room-calendar/room-calendar.component';
import { RoomAnalyticsComponent } from './reports/room-analytics/room-analytics.component';
import { EquipmentAnalyticsComponent } from './reports/equipment-analytics/equipment-analytics.component';
import { MyCalendarComponent } from './reports/my-calendar/my-calendar.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'contact', component: ContactComponent },
  { path: 'swaggerui', component: OpenApiListComponent, canActivate: [MsalGuard] },
  { path: 'roomevents', component: RoomCalendarComponent, canActivate: [MsalGuard] },
  { path: 'roomanalytics', component: RoomAnalyticsComponent, canActivate: [MsalGuard] },
  { path: 'equipmentanalytics', component: EquipmentAnalyticsComponent, canActivate: [MsalGuard] },
  { path: 'calendar', component: MyCalendarComponent, canActivate: [MsalGuard] },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})

export class RoomFinderHomeRoutingModule { }
