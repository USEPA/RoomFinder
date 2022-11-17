import { Component, OnInit, ViewChild, NgZone, Input } from '@angular/core';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { SelectionModel } from '@angular/cdk/collections';
import { MatTableDataSource, MatTable } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MailboxesEntity, LocationContext, Resource, EquipmentDependenciesEntity } from '../shared/models/mailboxes.model';
import { FormVtcComponent } from './forms/form-vtc.component';
import { ResourceStatusFilterService } from '../shared/services/resource-status-filter-service';
import { MatExpansionPanel } from '@angular/material/expansion';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomService } from '../shared/services/room-service';
import { RoomFilterEntity, RoomFilterResults } from '../shared/models/resource-room.model';
import { Observable, Subject, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  AvailabilityRequestFilterEntity, AttendeeInfo, MeetingAttendeeType,
  AvailabilityRequestFilterAPI
} from '../shared/models/availabilityrequests.model';
import { FormAvailabilityRequestComponent } from './forms/form-availabilityrequest.component';
import { VTCDialogData } from '../shared/models/vtc.model';
import { RoomCitiesListComponent } from './room/room-cities-list.component';
import { RoomOfficesListComponent } from './room/room-offices-list.component';
import { RoomFloorsListComponent } from './room/room-floors-list.component';
import { LoggingService } from '../shared/services/logging-service';
import { AlertsService } from '../shared/services/alerts-service';
import { ResourceComponentBase } from './resource.component.base';

@Component({
  selector: 'app-rooms-list',
  templateUrl: './rooms-list.component.html',
  styleUrls: ['./addin.component.scss'],
  animations: [
    trigger('detailExpand', [
      state('collapsed', style({ height: '0px', minHeight: '0' })),
      state('expanded', style({ height: '*' })),
      transition(
        'expanded <=> collapsed',
        animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')
      )
    ])
  ]
})
export class RoomsListComponent extends ResourceComponentBase implements OnInit {
  @Input() locationTab = false;
  @ViewChild('filterPanel') filterPanel: MatExpansionPanel;
  @ViewChild('materialTable') materialTable: MatTable<any>;
  @ViewChild('citiesList') citiesList: RoomCitiesListComponent;
  @ViewChild('officesList') officesList: RoomOfficesListComponent;
  @ViewChild('floorsList') floorsList: RoomFloorsListComponent;

  public roomFilter: RoomFilterEntity = null;
  public availabilityRequestFilter: AvailabilityRequestFilterEntity = null;
  public dataSource: MatTableDataSource<RoomFilterResults>;

  headersToDisplay = [
    { value: 'select', display: '' },
    { value: 'restrictionImage', display: '' },
    { value: 'availabilityImage', display: '' },
    { value: 'displayName', display: 'Name' },
    { value: 'capacity', display: 'Seats' }
  ];

  public row: any;
  public expandedElement: MailboxesEntity | null;
  public selectionRooms = new SelectionModel(true, []);
  public selectionEquipment = new SelectionModel(true, []);
  public selectLocation: any;
  public capacity: number;
  private roomDisplayName: string;
  private startTime: Date;
  private endTime: Date;
  public selectState: any;
  public selectCity: any;
  public selectOffice: any;
  public selectFloor: any;
  private equipmentDependencies: string[] = [];
  private unsubroomfilter = new Subject();
  public loading$: Observable<boolean>;


  constructor(
    public dialog: MatDialog,
    public zone: NgZone,
    snackBar: MatSnackBar,
    private roomService: RoomService,
    resourceStatusFilterService: ResourceStatusFilterService,
    private loggingService: LoggingService,
    private alertService: AlertsService
  ) {
    super(zone, snackBar, resourceStatusFilterService);
    this.setDataSource();
  }

  ngOnInit() {
    this.loading$ = of(false);
    this.headersToDisplay.forEach(header => {
      this.columnsToDisplay.push(header.value);
    });
    this.setDataSource();
  }

  checkboxLabel(row?: EquipmentDependenciesEntity, idx: number = 0): string {
    return `${
      this.selectionEquipment.isSelected(row.emailAddress) ? 'deselect' : 'select'
      } row ${idx + 1}`; // was row.position
  }

  onItemSelected(idx: number): void {
    console.log(`ItemSelected ${idx}`);
  }

  checkLocation(selectedLocation: any): void {
    this.selectLocation = selectedLocation;
  }

  checkStates(selectedState: any): void {
    this.selectState = selectedState;
    this.citiesList.refreshCities(this.selectState);
  }

  checkCities(selectedCity: any): void {
    this.selectCity = selectedCity;
    this.officesList.refreshOffices(this.selectState, this.selectCity);
  }

  checkOffices(selectedOffice: any): void {
    this.selectOffice = selectedOffice;
    this.floorsList.refreshFloors(this.selectState, this.selectCity, this.selectOffice);
  }

  checkFloors(selectedFloor: any): void {
    this.selectFloor = selectedFloor;
  }

  checkEquipmentConfig(equipmentConfigFilter): void {
    this.equipmentDependencies = [];
    equipmentConfigFilter.forEach(element => {
      if (element.checked) {
        this.equipmentDependencies.push(element.value);
      }
    });
  }

  checkEquipmentToggle(equipment: EquipmentDependenciesEntity) {
    this.selectionEquipment.toggle(equipment);
  }

  checkRoomToggle(row: any) {
    this.selectionRooms.toggle(row);
  }

  expandoRow(element: any, row: any) {
    this.expandedElement = this.expandedElement === element ? null : element || this.selectionRooms.toggle(row);
  }

  expandoRowCss(element: any) {
    return element === this.expandedElement ? 'expanded' : 'collapsed';
  }

  protected setDataSource(roomResults?: RoomFilterResults[]) {
    if (roomResults) {
      this.dataSource = new MatTableDataSource<RoomFilterResults>(roomResults);
      this.loading$ = of(false);
    } else {
      this.dataSource = new MatTableDataSource<RoomFilterResults>([]);
    }
  }

  get displayDataSource() {
    return this.dataSource.data.length > 0;
  }

  protected async loadRoomsWithTimeBlock() {
    this.setDataSource();
    this.loading$ = of(true);
    const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;

    // tslint:disable-next-line: max-line-length
    this.getAppointmentStartDate(
      Office.context.mailbox.item,
      (eventError: any) => {
        this.loggingService.logError(eventError, [
          `AddIn:User=${currentUserEmail}`, 'loadRoomsWithTimeBlock-EWS Error-StartDate',
        ]);
      },
      (eventStartTime: Date) => {
        this.zone.run(() => {
          // tslint:disable-next-line: max-line-length
          this.getAppointmentEndDate(
            Office.context.mailbox.item,
            (eventError: any) => {
              this.loggingService.logError(eventError, [
                `AddIn:User=${currentUserEmail}`, 'loadRoomsWithTimeBlock-EWS Error-EventDate'
              ]);
            },
            (eventEndTime: Date) => {
              this.zone.run(() => {
                this.startTime = eventStartTime;
                this.endTime = eventEndTime;
                this.roomFilter.start = this.startTime.toUTCString();
                this.roomFilter.end = this.endTime.toUTCString();
                this.roomService.postFindRoomAvailability(this.roomFilter)
                  .pipe(takeUntil(this.unsubroomfilter))
                  .subscribe(
                    rooms => {
                      this.setDataSource(rooms);
                      this.loading$ = of(false);
                      this.displayProgressSpinner = false;
                    },
                    err => this.loggingService.logInformation(`${err}`, [
                      `AddIn:User=${currentUserEmail}`, 'loadRoomsWithTimeBlock-EWS Error-HTTP Error']),
                    () => this.loggingService.logInformation('HTTP request completed.', [
                      `AddIn:User=${currentUserEmail}`, 'loadRoomsWithTimeBlock-getRoomFilterFromAPI'])
                  );
              });
            }
          );
        });
      }
    );
  }

  private getOfficeItem() {
    this.roomFilter = {
      includeUnavailable: this.includeUnavailable,
      includeRestricted: this.includeRestricted,
      start: '',
      end: '',
      state: this.selectState,
      city: this.selectCity,
      office: !this.locationTab ? this.selectLocation : this.selectOffice,
      floor: this.selectFloor,
      listPath: '',
      requiredEquipment: this.equipmentDependencies,
      capacity: this.capacity
    };

    this.loading$ = of(true);
    this.loadRoomsWithTimeBlock();
  }

  applyFilter(): void {
    this.search();
  }

  // Search Button
  search(): void {
    if (this.filterPanel.expanded) {
      this.filterPanel.close();
    }
    this.displayProgressSpinner = true;
    this.getOfficeItem();
  }

  protected openDialogAvailabilityRequest(availabilityRequest: AvailabilityRequestFilterAPI[]): void {
    const dialogRef = this.dialog.open(FormAvailabilityRequestComponent, {
      width: '300px',
      height: '900px',
      data: {
        availabilityRequest, displayName: this.roomDisplayName,
        startTime: this.startTime.toLocaleTimeString(),
        endTime: this.endTime.toLocaleTimeString()
      }
    });

    dialogRef.afterClosed().subscribe(() => {
    });
  }

  protected openDialogVTC(): void {
    const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;
    const dialogRef = this.dialog.open(FormVtcComponent, {
      width: '250px',
      data: {}
    });

    dialogRef.afterClosed().subscribe(result => {
      const vtcResult: VTCDialogData = result;

      let conferenceTypeSelected = '';
      // User Selected Cancel Button
      if (vtcResult === undefined) {
        return;
      }
      vtcResult.conferenceTypes.forEach(element => {
        if (element.checked) {
          conferenceTypeSelected = conferenceTypeSelected + element.name + ', ';
        }
      });

      let item: any;
      item = Office.context.mailbox.item;
      const emailMessage = `<h3>VTC Information</h3>
            </br><p><b>Name</b>: ${vtcResult.firstLastName}
            </br><b>Phone Number:</b> ${vtcResult.phoneNumber}
            </br><b>Email Address:</b> ${vtcResult.emailAddress}
            </br><b>Conference Start Date/Time:</b> ${this.startTime.toLocaleString()}
            </br><b>Conference End Date/Time:</b> ${this.endTime.toLocaleString()}
            </br><b>Number of Attendees:</b> ${vtcResult.numberOfAttendees}
            </br><b>Purpose of Conference:</b> ${vtcResult.purposeOfConference}
            </br><b>Conference Type:</b> ${conferenceTypeSelected}
            </br><b>Audio Connection Information:</b> ${
        vtcResult.audioConnectionInfo
        }
            </br><b>Location of Host:</b> ${vtcResult.locationOfHost}
            </br><b>Location(s) Connected With:</b> ${
        vtcResult.locationsConnectedWith
        }
            </br><b>Comment:</b> ${vtcResult.comment}
            </p>`;
      item.body.setAsync(
        emailMessage,
        { coercionType: Office.CoercionType.Html },
        (asyncResult: Office.AsyncResult<any>) => {
          this.loggingService.logInformation(`Office.AsyncResult: ${asyncResult.status}`, [
            `AddIn:User=${currentUserEmail}`, 'openDialogVTC-item.body.setAsync']);
        }
      );
    });
  }

  protected addVTCLink(): void {
    const vtcLinkURL = 'https://forms.office.com/Pages/ResponsePage.aspx?id=s3iziEhnZ0is-Xaqy-ympzORFWL0Zw9AgOPoqvrYJidUMVk5OUI2NkpWVEFRRlhaR1NVSFpXV1pUNyQlQCN0PWcu';
    const vtcLinkDisplay = 'Click here to submit Video Teleconference (VTC) Request Form';
    const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;

    let item: any;
    item = Office.context.mailbox.item;
    const emailMessage = `<a href="${vtcLinkURL}">${vtcLinkDisplay}</a>`;
    item.body.setAsync(
      emailMessage,
      { coercionType: Office.CoercionType.Html },
      (asyncResult: Office.AsyncResult<any>) => {
        this.loggingService.logInformation(`Office.AsyncResult: ${asyncResult.status}`, [
          `AddIn:User=${currentUserEmail}`, 'addVTCLink-item.body.setAsync']);
      }
    );
  }

  recurringAvailabilityCheck(): void {
    const attendeeInfo: AttendeeInfo[] = [];
    const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;
    this.displayProgressSpinner = true;

    if (this.selectionRooms.selected.length > 0) {
      const selectedRoom: RoomFilterResults[] = this.selectionRooms.selected;
      selectedRoom.forEach(item => {
        this.roomDisplayName = item.displayName;
        attendeeInfo.push({
          smtpAddress: item.emailAddress,
          attendeeType: MeetingAttendeeType.Room, excludeConflicts: false
        });
      });
    } else {
      this.snackbarMessage('Room not selected.', 'Warning', 3000);
      this.loggingService.logWarning('Room not selected.', [
        `AddIn:User=${currentUserEmail}`, 'recurringAvailabilityCheck']);
      return;
    }

    this.getRecurrence(
      Office.context.mailbox.item.recurrence,
      (eventError: any) => {
        this.loggingService.logError(eventError, [
          `AddIn:User=${currentUserEmail}`, 'recurringAvailabilityCheck-Office.context.mailbox.item.recurrence']);
      },
      (recurrenceItem: Office.Recurrence) => {
        this.zone.run(() => {
          // tslint:disable-next-line: max-line-length
          if (recurrenceItem === null) {
            this.snackbarMessage('Recurrence Pattern cannot be empty.', 'Error', 3000);
            this.loggingService.logError('Recurrence Pattern cannot be empty', [
              `AddIn:User=${currentUserEmail}`, 'recurringAvailabilityCheck'
            ]);
            return;
          }
          this.availabilityRequestFilter = {
            setIndex: 0,
            setSize: -1,
            attendees: attendeeInfo,
            recurrence: recurrenceItem,
            roomFilter: this.roomFilter
          };

          this.roomService
            .getCheckRecurrence(this.availabilityRequestFilter)
            .pipe(takeUntil(this.unsubroomfilter))
            .subscribe(
              availability => {
                this.loading$ = of(false);
                this.openDialogAvailabilityRequest(availability);
              },
              err => {
                this.alertService.add(`${err.message} with status ${err.status}`, 'Room-Error');
                this.loggingService.logError(`${err.status}:${err.message}`, [
                  `AddIn:User=${currentUserEmail}`, 'recurringAvailabilityCheck-getAvailabilityRequestFilterFromAPI'
                ]);
              },
              () => {
                this.displayProgressSpinner = false;
                this.loggingService.logInformation('HTTP request completed.', [
                  `AddIn:User=${currentUserEmail}`, 'recurringAvailabilityCheck'
                ]);
              }
            );
        });
      }
    );
  }

  addToInvite(): void {
    let item: any;
    item = Office.context.mailbox.item;
    const currentUserEmail = Office.context.mailbox.userProfile.emailAddress;

    if (this.selectionRooms.selected.length === 0) {
      this.snackbarMessage('Room not selected.', 'Warning', 3000);
      this.loggingService.logInformation('Room not selected', [
        `AddIn:User=${currentUserEmail}`, 'addToInvite'
      ]);
      return;
    }

    let equipmentIsVTC = false;
    this.selectionEquipment.selected.forEach(element => {
      if (element.equipmentType === 'VTC') {
        equipmentIsVTC = true;
      }
    });

    const locationContext: LocationContext = {
      equipments: this.selectionEquipment.selected,
      rooms: this.selectionRooms.selected
    };

    // Clear Selected Items
    this.selectionRooms.clear();
    this.selectionEquipment.clear();

    item.location.getAsync(
      { asyncContext: locationContext },
      (location: Office.AsyncResult<any>) => {
        if (location.status === Office.AsyncResultStatus.Succeeded) {
          const resources = location.value.split(';');
          let newLocation = '';
          const itemContext: LocationContext = location.asyncContext;

          itemContext.rooms.forEach(newAttendee => {
            console.log('newAttendee');
            console.log(newAttendee);

            let found = false;

            resources.forEach(currentAttendee => {
              if (currentAttendee.trim() === newAttendee.displayName) {
                found = true;
                if (newLocation !== null && newLocation.length > 0) {
                  newLocation += '; ';
                }
                newLocation += newAttendee.displayName;
              }
            });

            if (!found) {
              item.requiredAttendees.getAsync((ra: { value: any[] }) => {
                for (const radx of ra.value) {
                  if (radx.emailAddress === newAttendee.emailAddress) {
                    this.loggingService.logInformation(`Email Address Found. Do not add. ${radx.emailAddress}`, [
                      `AddIn:User=${currentUserEmail}`, 'addToInvite',
                    ]);
                    found = true;
                  }
                }

                if (!found) {
                  // Note: per http://dev.outlook.com/reference/add-ins/Office.context.mailbox.item.html#resources, as of Aug 16,2016
                  //       item.resources is read mode only
                  // As of the time of writing this code (Aug 2016), the Outlook behavior when adding a room to "Required Attendees" is that
                  // it moves it automatically to Resources.  However, at least the way the equipment resources are currently configured in
                  // Exhange/Active Directory, the equipment resources are not moved to Resources and stay as RequiredAttendees
                  // Not sure if it's customer setup specific or not -- a Premier case will be opened TODO
                  // if (vm.SearchType() === 'Room') {
                  if (newLocation !== null && newLocation.length > 0) {
                    newLocation += '; ';
                  }
                  newLocation += newAttendee.displayName;
                  console.log(`Location Email: ${newAttendee.emailAddress}`);

                  const locations = [
                    {
                      id: newAttendee.emailAddress,
                      type: Office.MailboxEnums.LocationType.Room
                    }
                  ];
                  item.enhancedLocation.addAsync(locations, asyncRes => {
                    if (asyncRes.status === Office.AsyncResultStatus.Failed) {
                      this.loggingService.logError(`Failed to process ${asyncRes.error.message}`, [
                        `AddIn:User=${currentUserEmail}`, 'addToInvite',
                      ]);
                      this.alertService.add(asyncRes.error.message, 'Room-Error');
                    }
                  });

                  const addRoomAttendee: Resource = {
                    displayName: newAttendee.displayName,
                    emailAddress: newAttendee.emailAddress
                  };
                  item.requiredAttendees.addAsync(
                    [addRoomAttendee],
                    (reqAttendees: Office.AsyncResult<any>) => {
                      if (
                        reqAttendees.status === Office.AsyncResultStatus.Failed
                      ) {
                        this.loggingService.logError(reqAttendees.error.message, [
                          `AddIn:User=${currentUserEmail}`, 'addToInvite-Location'
                        ]);
                        this.alertService.add(reqAttendees.error.message, 'Room-Error');
                      }
                    }
                  );
                }
              }); // end of requiredAttendees.getAsync
            }
          }); // end of foreach selected row

          itemContext.equipments.forEach((newAttendee) => {
            let found = false;

            resources.forEach((currentAttendee) => {
              if (currentAttendee.trim() === newAttendee.displayName) {
                found = true;
                if (newLocation !== null && newLocation.length > 0) {
                  newLocation += '; ';
                }
                newLocation += newAttendee.displayName;
              }
            });

            if (!found) {
              item.requiredAttendees.getAsync(ra => {
                for (const radx of ra.value) {
                  if (radx.emailAddress === newAttendee.emailAddress) {
                    found = true;
                  }
                }
                if (!found) {
                  const addEquipmentAttendee: Resource = {
                    displayName: newAttendee.displayName,
                    emailAddress: newAttendee.emailAddress
                  };
                  item.requiredAttendees.addAsync(
                    [addEquipmentAttendee],
                    (reqAttendees: Office.AsyncResult<any>) => {
                      if (
                        reqAttendees.status === Office.AsyncResultStatus.Failed
                      ) {
                        this.loggingService.logError(reqAttendees.error.message, [
                          `AddIn:User=${currentUserEmail}`, 'addToInvite-Location',
                        ]);
                        this.alertService.add(reqAttendees.error.message, 'Room-Error');
                      }
                    }
                  );
                }
              }); // end of requiredAttendees.getAsync
            }
          }); // end of foreach selected row
        } else {
          this.loggingService.logError(`Unable to get location.  Error: ${location.error.message}`, [
            `AddIn:User=${currentUserEmail}`, 'addToInvite-Location',
          ]);
          this.alertService.add(location.error.message, 'Room-Error');
        }
      }
    );
    if (equipmentIsVTC) {
      this.addVTCLink();
      // NOTE: Modal Dialog does not render correctly when called via async
      // MOVE TO NEXT PHASE ---- this.openDialogVTC();
    }
  }
}
