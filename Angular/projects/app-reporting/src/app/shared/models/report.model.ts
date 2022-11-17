
export class MeetingTimeWindowModel {
  startDate: Date;
  endDate: Date;
}

export class EquipmentAnalyticsAPI {
  equipmentName: string;
  numberOfMeetings: number;
}

export interface ResourcesCoreEntity {
  samAccountName?: string;
  displayName: string;
  emailAddress: string;
}

export class RoomEventsEntity extends MeetingTimeWindowModel {
  constructor() {
    super();
    this.resources = null;
  }
  emailAddress: string;
  resources?: any[];
}

export class RoomEventsAPI {
  mailboxId: string;
  location?: string;
  locationEmail?: string;
  organizerName?: string;
  organizerEmail?: string;
  subject?: string;
  startTime: Date;
  endTime: Date;
  startTimeString?: string;
  endTimeString?: string;
  isMeeting?: boolean;
  isOnlineMeeting?: boolean;
  isAllDayEvent?: boolean;
  isRecurring?: boolean;
  isCancelled?: boolean;
  timeZone: string;
}

export class RoomAnalyticsAPI {
  roomName: string;
  numberOfMeetings: number;
}
