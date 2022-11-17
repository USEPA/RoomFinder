import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable, of } from 'rxjs';
import { LoggingService } from '../shared/services/logging-service';

@Component({
  selector: 'app-addin',
  templateUrl: './addin.component.html',
  styleUrls: ['./addin.component.scss']
})
export class AddInComponent implements OnInit, OnDestroy {
  loadingOfficeAddIn$: Observable<boolean>;

  constructor(public loggingService: LoggingService) { }

  ngOnInit() {
    this.onLoadOfficeAddIn();
  }

  ngOnDestroy() {
    // Only need to unsubscribe if its a multi event Observable
    this.loadingOfficeAddIn$ = of(false);
  }

  onLoadOfficeAddIn() {
    Office.initialize = reason => {
      document.getElementById('sideload-msg').style.display = 'none';
      const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;
      this.loggingService.logInformation(`Office Initialized ${reason}`, [
        `AddIn:User=${currentUserEmail}`,
        'ngOnInit']);
      this.loadingOfficeAddIn$ = of(true);
    };
  }
}
