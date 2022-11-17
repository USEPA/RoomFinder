import { Checkboxes } from './matcheckbox.model';

export interface VTCDialogData {
  firstLastName: string;
  phoneNumber: string;
  emailAddress: string;
  conferenceDate: string;
  conferenceStartTime: any;
  conferenceEndTime: any;
  numberOfAttendees: string;
  purposeOfConference: string;
  conferenceTypes: Checkboxes[];
  audioConnectionInfo: string;
  locationOfHost: string;
  locationsConnectedWith: string;
  comment: string;
}
