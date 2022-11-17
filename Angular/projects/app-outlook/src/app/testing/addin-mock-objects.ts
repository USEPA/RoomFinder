import { HttpErrorResponse } from '@angular/common/http';
import { IAppConfig } from '../shared/models/app-config.model';
import { DialogDataAvailabilityRequest, AttendeeAvailabilityInfoEntity } from '../shared/models/availabilityrequests.model';
import { Event } from '../shared/models/event.model';
import { RoomListing, RoomFilterResults, RoomFilterEntity } from '../shared/models/resource-room.model';
import { EquipmentAPI, EquipmentFilterResults, LocationAddress } from '../shared/models/resource-equipment.model';
import { MatOptionItem } from '../shared/models/matoption.model';
import { LocationsAPI } from '../shared/models/locations.model';
import { FilterBaseEntity } from '../shared/models/filter-base.model';
import { Alert } from '../shared/models/alert.model';

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


  public getAlerts(): Alert[] {
    const items: Alert[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push({ message: `message-${idx}`, debug: 'debug' });
    }
    return items;
  }

  public getResourceFilter(
    start: string,
    end: string,
    includeUnavailable: boolean = false,
    includeRestricted: boolean = false): FilterBaseEntity {
    const filter = new FilterBaseEntity(start, end);
    filter.includeRestricted = includeRestricted;
    filter.includeUnavailable = includeUnavailable;
    return filter;
  }

  public getResourceRoomFilter(
    start: string,
    end: string,
    capacity: number = 5,
    includeUnavailable: boolean = false,
    includeRestricted: boolean = false): RoomFilterEntity {
    const filter = this.getResourceFilter(start, end, includeUnavailable, includeRestricted);
    (filter as RoomFilterEntity).capacity = capacity;
    return (filter as RoomFilterEntity);
  }

  public getEquipmentFilter(numberOfMeetings: number = 5, eType: string = 'VTC'): EquipmentFilterResults[] {
    const items: EquipmentFilterResults[] = [];
    for (let idx = 0; idx < numberOfMeetings; idx++) {
      items.push({
        samAccountName: `sam${idx}`,
        emailAddress: `email@${idx}.com`,
        availabilityImage: 'image',
        restrictionImage: 'image',
        restrictionTooltip: 'tooltip',
        restrictionType: 0,
        dependencies: [],
        displayName: `display${idx}`,
        location: new LocationAddress(),
        equipmentType: eType
      });
    }
    return items;
  }

  public getEquipmentCities(): string[] {
    const items: string[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push(`city${idx}`);
    }
    return items;
  }

  public getEquipments(): EquipmentAPI[] {
    const items: EquipmentAPI[] = [];
    return items;
  }

  public getEquipmentTypes(): string[] {
    const items: string[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push(`equipmentType${idx}`);
    }
    return items;
  }

  public getEquipmentFloors(): MatOptionItem[] {
    const items: MatOptionItem[] = [];
    return items;
  }

  public getEquipmentOffices(): string[] {
    const items: string[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push(`office${idx}`);
    }
    return items;
  }

  public getEquipmentStates(): string[] {
    const items: string[] = [];
    for (let idx = 0; idx < 5; idx++) {
      items.push(`state${idx}`);
    }
    return items;
  }

  public getAttendeeAvailability(idx: number, status?: boolean): AttendeeAvailabilityInfoEntity {
    return {
      emailAddress: `test${idx}@email.com`,
      startTime: new Date('2019-10-02T01:00:00Z'),
      endTime: new Date('2019-10-02T01:30:00Z'),
      status
    };
  }

  public getMatDialogData(seedData: number = 5): DialogDataAvailabilityRequest {
    const attendees: AttendeeAvailabilityInfoEntity[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      const status = (idx % 2) === 0;
      attendees.push(this.getAttendeeAvailability(idx, status));
    }
    const data: DialogDataAvailabilityRequest = {
      availabilityRequest: {
        isLastSet: false,
        data: attendees,
        recurrencePattern: {}
      },
      displayName: 'request',
      startTime: '2019-10-02T01:00:00Z',
      endTime: '2019-10-02T01:30:00Z'
    };
    return data;
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

  public getRoomListings(seedData: number = 5): RoomListing[] {
    const items: RoomListing[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ emailAddress: `email${idx}`, displayName: `name-${idx}` });
    }
    return items;
  }

  public getRoomFilterResults(numberOfMeetings: number = 5): RoomFilterResults[] {
    const items: RoomFilterResults[] = [];
    for (let idx = 0; idx < numberOfMeetings; idx++) {
      items.push({
        samAccountName: `sam${idx}`,
        emailAddress: `email@${idx}.com`,
        capacity: (idx * 5),
        availabilityImage: 'available.ico',
        restrictionImage: 'image.ico',
        restrictionTooltip: 'tooltip.ico',
        restrictionType: 0,
        dependencies: [],
        equipmentDependencies: [],
        displayName: `display${idx}`
      });
    }
    return items;
  }

  public getRoomStatesFromAPI(seedData: number = 5): MatOptionItem[] {
    const items: MatOptionItem[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ value: `state${idx}`, viewValue: `viewstate${idx}` });
    }
    return items;
  }

  public getRoomOfficesFromAPI(seedData: number = 5): MatOptionItem[] {
    const items: MatOptionItem[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ value: `office${idx}`, viewValue: `viewoffice${idx}` });
    }
    return items;
  }

  public getRoomFloorsFromAPI(seedData: number = 5): MatOptionItem[] {
    const items: MatOptionItem[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ value: `floor${idx}`, viewValue: `viewfloor${idx}` });
    }
    return items;
  }

  public getLocationsFromAPI(seedData: number = 5): LocationsAPI[] {
    const items: LocationsAPI[] = [];
    for (let idx = 0; idx < seedData; idx++) {
      items.push({ id: idx, displayName: `viewfloor${idx}` });
    }
    return items;
  }
}
