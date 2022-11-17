import { Injectable } from '@angular/core';
import { Alert } from '../models/alert.model';

@Injectable({
  providedIn: 'root'
})
export class AlertsService {

  alerts: Alert[];
  constructor() {
    this.alerts = [];
  }

  add(message: string, debug: string = '') {
    return this.alerts.push({ message, debug });
  }

  remove(alert: Alert) {
    this.alerts.splice(this.alerts.indexOf(alert), 1);
  }

  removeByTitle(alertTitle: string) {
    const alert = this.alerts.find((val) => val.message.indexOf(alertTitle) > -1);
    this.alerts.splice(this.alerts.indexOf(alert), 1);
  }

  get() {
    return this.alerts;
  }
}
