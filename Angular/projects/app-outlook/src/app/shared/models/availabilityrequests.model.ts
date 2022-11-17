import { RoomFilterEntity } from './resource-room.model';

export enum MeetingAttendeeType {
    /// <summary>
    /// The attendee is the organizer of the meeting.
    /// </summary>
    Organizer,
    /// <summary>
    /// The attendee is required.
    /// </summary>
    Required,
    /// <summary>
    /// The attendee is optional.
    /// </summary>
    Optional,
    /// <summary>
    /// The attendee is a room.
    /// </summary>
    Room,
    /// <summary>
    /// The attendee is a resource.
    /// </summary>
    Resource
}

export interface AvailabilityRequestFilterEntity {
    setIndex: number;
    setSize: number;
    attendees: AttendeeInfo[];
    recurrence: Office.Recurrence;
    roomFilter: RoomFilterEntity;
}

export interface AttendeeInfo {
    smtpAddress: string;
    attendeeType: MeetingAttendeeType;
    excludeConflicts: boolean;
}

export interface RecurringAvailability {
    eventDate: string;
    availabilityImage: string;
}

export interface AttendeeAvailabilityInfoEntity {
    emailAddress: string;
    startTime: Date;
    endTime: Date;
    status?: boolean;
}

export class AvailabilityRequestFilterAPI {
    isLastSet: boolean;
    data: AttendeeAvailabilityInfoEntity[];
    recurrencePattern: any;
}

export interface DialogDataAvailabilityRequest {
    availabilityRequest: AvailabilityRequestFilterAPI;
    displayName: string;
    startTime: string;
    endTime: string;
}
