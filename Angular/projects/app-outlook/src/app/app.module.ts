import { NgModule, APP_INITIALIZER } from '@angular/core';
import { BrowserAnimationsModule, NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DatePipe } from '@angular/common';
import { AppComponent } from './app.component';
import { ResourceStatusFilterService } from 'projects/app-outlook/src/app/shared/services/resource-status-filter-service';
import { HttpClientModule } from '@angular/common/http';
import { RoomService } from 'projects/app-outlook/src/app/shared/services/room-service';
import { OverlayService } from './shared/services/overlay-service';
import { EquipmentService } from 'projects/app-outlook/src/app/shared/services/equipment-service';
import { LoggingService } from './shared/services/logging-service';
import { AppConfigService, appConfigFactory } from './shared/services/app.config.service';
import { SharedModule, DisableAnimation } from './shared/shared.module';
import { BrowserModule } from '@angular/platform-browser';
import { FormVtcComponent } from './addin/forms/form-vtc.component';
import { FormAvailabilityRequestComponent } from './addin/forms/form-availabilityrequest.component';
import { RoomStatesListComponent } from './addin/room/room-states-list.component';
import { RoomCitiesListComponent } from './addin/room/room-cities-list.component';
import { RoomFloorsListComponent } from './addin/room/room-floors-list.component';
import { RoomOfficesListComponent } from './addin/room/room-offices-list.component';
import { RoomsListComponent } from './addin/rooms-list.component';
import { EquipmentListComponent } from './addin/equipment-list.component';
import { LocationListComponent } from './addin/location-list/location-list.component';
import { EquipmentStatesListComponent } from './addin/equipment/equipment-states-list.component';
import { EquipmentCitiesListComponent } from './addin/equipment/equipment-cities-list.component';
import { EquipmentFloorsListComponent } from './addin/equipment/equipment-floors-list.component';
import { EquipmentOfficesListComponent } from './addin/equipment/equipment-offices-list.component';
import { EquipmentTypesListComponent } from './addin/equipment/equipment-types-list.component';
import { EquipmentConfigComponent } from './addin/equipment/equipment-config.component';
import { AddInRoomFinderModule } from './addin-room-finder/addin-room-finder.module';
import { ResourceStatusComponent } from './addin/resource-status/resource-status.component';
import { AddInComponent } from './addin/addin.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@NgModule({
  imports: [
    BrowserModule,
    AnimationModule(),
    HttpClientModule,
    NgbModule,
    SharedModule,
    AddInRoomFinderModule
  ],
  declarations: [
    AppComponent,
    AddInComponent,
    EquipmentCitiesListComponent,
    EquipmentConfigComponent,
    EquipmentFloorsListComponent,
    EquipmentListComponent,
    EquipmentOfficesListComponent,
    EquipmentStatesListComponent,
    EquipmentTypesListComponent,
    FormVtcComponent,
    FormAvailabilityRequestComponent,
    LocationListComponent,
    RoomCitiesListComponent,
    RoomFloorsListComponent,
    RoomOfficesListComponent,
    RoomStatesListComponent,
    RoomsListComponent,
    ResourceStatusComponent,
  ],
  entryComponents: [
    FormVtcComponent,
    FormAvailabilityRequestComponent,
    AddInComponent,
  ],
  exports: [
  ],
  providers: [
    LoggingService,
    OverlayService,
    RoomService,
    EquipmentService,
    ResourceStatusFilterService,
    DatePipe,
    AppConfigService, {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (configService: AppConfigService) => appConfigFactory(configService),
      deps: [AppConfigService]
    },
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  constructor() {
  }
}

export function AnimationModule(): any {
  return DisableAnimation() ? NoopAnimationsModule : BrowserAnimationsModule;
}
