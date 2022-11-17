import { NgModule, APP_INITIALIZER } from '@angular/core';
import { BrowserAnimationsModule, NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DatePipe } from '@angular/common';
import { AppRoutingModule } from './app-routing.module';
import { FontAwesomeModule, FaIconLibrary } from '@fortawesome/angular-fontawesome';
import { faCoffee } from '@fortawesome/free-solid-svg-icons';
import { faTicketAlt } from '@fortawesome/free-solid-svg-icons';
import { faCalendarWeek } from '@fortawesome/free-solid-svg-icons';
import { faHome } from '@fortawesome/free-solid-svg-icons';
import { faArrowCircleDown } from '@fortawesome/free-solid-svg-icons';
import { faPhone } from '@fortawesome/free-solid-svg-icons';
import { faStickyNote } from '@fortawesome/free-solid-svg-icons';
import { faSignInAlt } from '@fortawesome/free-solid-svg-icons';
import { faSignOutAlt } from '@fortawesome/free-solid-svg-icons';
import { faUser } from '@fortawesome/free-solid-svg-icons';
import { faCloud } from '@fortawesome/free-solid-svg-icons';
import { MsalModule, MsalService, MSAL_CONFIG, MSAL_CONFIG_ANGULAR, MsalInterceptor } from '@azure/msal-angular';
import { AppComponent } from './app.component';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { OverlayService } from './shared/services/overlay-service';
import { LoggingService } from './shared/services/logging-service';
import { ReportingService } from './shared/services/reporting-service';
import { AppConfigService, msalConfigFactory, msalConfigAngularFactory, appConfigFactory } from './shared/services/app.config.service';
import { SharedModule, DisableAnimation } from './shared/shared.module';
import { RoomFinderModule } from './room-finder/room-finder.module';
import { BrowserModule } from '@angular/platform-browser';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@NgModule({
  imports: [
    AppRoutingModule,
    BrowserModule,
    AnimationModule(),
    HttpClientModule,
    FontAwesomeModule,
    NgbModule,
    MsalModule,
    SharedModule,
    RoomFinderModule,
  ],
  declarations: [
    AppComponent,
  ],
  entryComponents: [],
  exports: [
  ],
  providers: [
    LoggingService,
    OverlayService,
    ReportingService,
    DatePipe,
    AppConfigService, {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (configService: AppConfigService) => appConfigFactory(configService),
      deps: [AppConfigService]
    },
    MsalService,
    {
      provide: MSAL_CONFIG,
      useFactory: msalConfigFactory
    },
    {
      provide: MSAL_CONFIG_ANGULAR,
      useFactory: msalConfigAngularFactory
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true
    },
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  constructor(library: FaIconLibrary) {
    library.addIcons(faCoffee, faTicketAlt,
      faCalendarWeek, faHome, faArrowCircleDown,
      faPhone, faStickyNote,
      faSignInAlt, faSignOutAlt,
      faUser, faCloud);
  }
}

export function AnimationModule(): any {
  return DisableAnimation() ? NoopAnimationsModule : BrowserAnimationsModule;
}
