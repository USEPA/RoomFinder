import { Component, OnInit, ViewChild, Input, NgZone } from '@angular/core';
import { MatExpansionPanel } from '@angular/material/expansion';
import { MatTableDataSource } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EquipmentCitiesListComponent } from './equipment/equipment-cities-list.component';
import { EquipmentOfficesListComponent } from './equipment/equipment-offices-list.component';
import { EquipmentFloorsListComponent } from './equipment/equipment-floors-list.component';
import { Subject, Observable, of } from 'rxjs';
import { ResourceStatusFilterService } from '../shared/services/resource-status-filter-service';
import { takeUntil } from 'rxjs/operators';
import { SelectionModel } from '@angular/cdk/collections';
import { FilterBaseEntity } from '../shared/models/filter-base.model';
import { EquipmentFilterResults } from '../shared/models/resource-equipment.model';
import { EquipmentService } from '../shared/services/equipment-service';
import { Resource } from '../shared/models/mailboxes.model';
import { LoggingService } from '../shared/services/logging-service';
import { ResourceComponentBase } from './resource.component.base';

@Component({
  selector: 'app-equipment-list',
  templateUrl: './equipment-list.component.html',
  styleUrls: ['./addin.component.scss']
})
export class EquipmentListComponent extends ResourceComponentBase implements OnInit {
  @Input() locationTab = false;
  @ViewChild('filterPanel') filterPanel: MatExpansionPanel;
  @ViewChild('citiesList') citiesList: EquipmentCitiesListComponent;
  @ViewChild('officesList') officesList: EquipmentOfficesListComponent;
  @ViewChild('floorsList') floorsList: EquipmentFloorsListComponent;

  public equipmentFilter: FilterBaseEntity = null;
  public dataSource: MatTableDataSource<EquipmentFilterResults>;

  headersToDisplay = [
    { value: 'select', display: '' },
    { value: 'restrictionImage', display: '' },
    { value: 'availabilityImage', display: '' },
    { value: 'displayName', display: 'Name' }
  ];

  public row: any;
  public selectionEquipment = new SelectionModel(true, []);
  public selectLocation: any;
  public capacity: any;
  private startTime: Date;
  private endTime: Date;
  public selectEquipmentType: any;
  public selectState: any;
  public selectCity: any;
  public selectOffice: any;
  public selectFloor: any;
  private equipmentDependencies?: string[];
  private unsubequipmentfilter = new Subject();
  public loading$: Observable<boolean>;


  constructor(
    public zone: NgZone,
    snackBar: MatSnackBar,
    public equipmentService: EquipmentService,
    resourceStatusFilterService: ResourceStatusFilterService,
    public loggingService: LoggingService
  ) {
    super(zone, snackBar, resourceStatusFilterService);
  }

  ngOnInit() {
    this.loading$ = of(false);
    this.headersToDisplay.forEach(header => {
      this.columnsToDisplay.push(header.value);
    });
    this.setDataSource();
  }

  protected checkboxLabel(row?): string {
    return `${
      this.selectionEquipment.isSelected(row.emailAddress) ? 'deselect' : 'select'
      } row ${row.position + 1}`;
  }

  checkEquipmentTypes(selectedEquipmentType: any): void {
    this.selectEquipmentType = selectedEquipmentType;
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

  protected setDataSource(equipmentResults?: EquipmentFilterResults[]): void {
    if (equipmentResults) {
      this.dataSource = new MatTableDataSource<EquipmentFilterResults>(equipmentResults);
      this.loading$ = of(false);
    } else {
      this.dataSource = new MatTableDataSource<EquipmentFilterResults>([]);
    }
  }

  public getOfficeContextMailboxItem() {
    return Office.context.mailbox.item;
  }

  public getOfficeContextUserEmailAddress() {
    return Office.context.mailbox.userProfile.emailAddress;
  }

  protected async loadEquipmentWithTimeBlock() {
    this.setDataSource();
    this.loading$ = of(true);
    const currentUserEmail = this.getOfficeContextUserEmailAddress();
    this.getAppointmentStartDate(
      this.getOfficeContextMailboxItem(),
      (eventError: any) => {
        this.loggingService.logError(eventError, [
          `AddIn:User=${currentUserEmail}`, 'loadEquipmentWithTimeBlock-EWS Error-StartDate'
        ]);
      },
      (eventStartTime: Date) => {
        this.zone.run(() => {
          this.getAppointmentEndDate(
            this.getOfficeContextMailboxItem(),
            (eventError: any) => {
              this.loggingService.logError(eventError, [
                `AddIn:User=${currentUserEmail}`, 'loadEquipmentWithTimeBlock-EWS Error-EndDate',
              ]);
            },
            (eventEndTime: Date) => {
              this.zone.run(() => {
                this.startTime = eventStartTime;
                this.endTime = eventEndTime;
                this.equipmentFilter.start = this.startTime.toUTCString();
                this.equipmentFilter.end = this.endTime.toUTCString();

                this.equipmentService
                  .postEquipmentFind(this.equipmentFilter)
                  .pipe(takeUntil(this.unsubequipmentfilter))
                  .subscribe(
                    equipment => {
                      this.setDataSource(equipment);
                      this.loading$ = of(false);
                      this.displayProgressSpinner = false;
                    },
                    err => this.loggingService.logError(err, [
                      `AddIn:User=${currentUserEmail}`, 'loadEquipmentWithTimeBlock-EWS Error-HTTP Error']),
                    () => this.loggingService.logInformation('HTTP request completed.', [
                      `AddIn:User=${currentUserEmail}`, 'loadEquipmentWithTimeBlock-postEquipmentFind'])
                  );
              });
            }
          );
        });
      }
    );
  }

  private getOfficeItem(): void {
    if (this.selectEquipmentType !== undefined) {
      this.equipmentDependencies = [this.selectEquipmentType];
    }
    this.equipmentFilter = {
      includeUnavailable: this.includeUnavailable,
      includeRestricted: this.includeRestricted,
      start: '',
      end: '',
      state: this.selectState,
      city: this.selectCity,
      office: this.selectOffice,
      floor: this.selectFloor,
      listPath: '',
      requiredEquipment: !this.locationTab ? this.equipmentDependencies : null
    };

    this.loading$ = of(true);
    this.loadEquipmentWithTimeBlock();
  }

  // Search Button
  public search(): void {
    if (this.filterPanel.expanded) {
      this.filterPanel.close();
    }
    this.displayProgressSpinner = true;
    this.getOfficeItem();
  }


  addToInvite(): void {
    let found = false;
    const item = this.getOfficeContextMailboxItem();
    const currentUserEmail = this.getOfficeContextUserEmailAddress();

    if (this.selectionEquipment.selected.length === 0) {
      this.snackbarMessage('Equipment not selected.', 'Warning', 3000);
      this.loggingService.logInformation('Equipment not selected', [
        `AddIn:User=${currentUserEmail}`, 'addToInvite']);
      return;
    }

    this.selectionEquipment.selected.forEach(element => {
      item.requiredAttendees.getAsync(ra => {
        for (const radx of ra.value) {
          if (radx.emailAddress === element.emailAddress) {
            found = true;
          }
        }
        if (!found) {
          const addEquipmentAttendee: Resource = {
            displayName: element.DisplayName,
            emailAddress: element.emailAddress
          };
          item.requiredAttendees.addAsync(
            [addEquipmentAttendee],
            (reqAttendees: Office.AsyncResult<any>) => {
              if (
                reqAttendees.status === Office.AsyncResultStatus.Failed
              ) {
                this.snackbarMessage(reqAttendees.error.message, 'Equipment Error', 3000);
                this.loggingService.logError(`Failed to process: ${reqAttendees.error.message}`, [
                  `AddIn:User=${currentUserEmail}`, 'addToInvite']);
              }
            }
          );
        }
      }); // end of requiredAttendees.getAsync
    });

    // Clear Selected Items
    this.selectionEquipment.clear();
  }
}
