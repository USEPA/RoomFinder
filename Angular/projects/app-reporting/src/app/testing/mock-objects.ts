import { HttpErrorResponse } from '@angular/common/http';
import { IAppConfig } from '../shared/models/app-config.model';
import { User } from '../shared/models/user.model';
import { Alert } from '../shared/models/alert.model';
import { EquipmentAnalyticsAPI, RoomEventsAPI, RoomAnalyticsAPI } from '../shared/models/report.model';
import { Event } from '../shared/models/event.model';
import * as MsalLib from 'msal';
import { ResourcesCoreEntity } from '../shared/models/report.model';

export class ObjectMocks {
  /**
   * Class to centralize mock objects so we don't need to repeat this in all test classes.
   */
  constructor() { }

  getHttpError(statusInt: number, message: string): HttpErrorResponse {
    return new HttpErrorResponse({
      status: statusInt,
      statusText: message
    });
  }

  public getAppConfig() {
    const settings: IAppConfig = {
      env: {
        name: 'dev',
        version: '0.0.0.0',
      },
      auth: {
        baseWebApiUrl: 'http://localhost/',
        audience: 'api:/guid/access_as_user',
        clientId: '00000000-0000-0000-0000-000000000000'
      }
    };
    return settings;
  }

  public getAuthAccount(): MsalLib.Account {
    const tokens: { [key: string]: string } = {};
    tokens.idtoken = 'asdfasdf213r23fasdfasdf';
    tokens.accesscode = 'helloconstants';
    const item = new MsalLib.Account('account1', 'home', 'joe@upn.com', 'joe', tokens, 'sid', 'prod');
    return item;
  }

  public getUser(): User {
    const user = new User();
    user.avatar = 'blue';
    user.displayName = 'tester jones';
    user.emailAddress = 'tester.jones@me.com';
    return user;
  }

  public getAlerts(): Alert[] {
    const items: Alert[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push({ message: `message-${idx}`, debug: 'debug' });
    }
    return items;
  }

  public getRoomListing(seedData: number = 5): ResourcesCoreEntity[] {
    const items: ResourcesCoreEntity[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ emailAddress: `email${idx}`, displayName: `name-${idx}` });
    }
    return items;
  }

  public getReportEquipments(seedData: number = 5) {
    const items: EquipmentAnalyticsAPI[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      const status = ((idx % 2) * 2);
      items.push({ equipmentName: `equipment-${idx}`, numberOfMeetings: status });
    }
    return items;
  }

  public getGraphEvents(seedData: number = 5): Event[] {
    const items: Event[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({
        subject: `subject${idx}`,
        organizer: { emailAddress: { name: 'name', address: 'email' } },
        start: { dateTime: '2019-10-01T01:00:00Z', timeZone: 'UTC' },
        end: { dateTime: '2019-10-01T01:30:00Z', timeZone: 'UTC' },
      });
    }
    return items;
  }

  public getRoomAnalytics(seedData: number = 10): RoomAnalyticsAPI[] {
    const items: RoomAnalyticsAPI[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      const status = ((idx % 2) * 2);
      items.push({ roomName: `room${idx}`, numberOfMeetings: status });
    }
    return items;
  }

  public getReportRoomEvents(seedData: number = 10): RoomEventsAPI[] {
    const items: RoomEventsAPI[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      const endDate = new Date();
      endDate.setHours(endDate.getHours() + 1);
      items.push({ mailboxId: `mailbox${idx}`, location: 'location', startTime: new Date(), endTime: endDate, timeZone: 'UTC' });
    }
    return items;
  }
}
