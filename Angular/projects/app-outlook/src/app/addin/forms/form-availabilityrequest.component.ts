import { Component, OnInit, Inject, ViewChild } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import {
  RecurringAvailability,
  AvailabilityRequestFilterAPI,
  DialogDataAvailabilityRequest
} from '../../shared/models/availabilityrequests.model';

@Component({
  selector: 'app-form-availabilityrequest',
  templateUrl: './form-availabilityrequest.component.html',
  styleUrls: ['./form-availabilityrequest.component.scss']
})
export class FormAvailabilityRequestComponent implements OnInit {
  public displayedColumns: string[] = ['eventDate', 'availabilityImage'];
  public dataSource: MatTableDataSource<RecurringAvailability>;
  private recurringAvailability: RecurringAvailability[] = [];
  public availabilityRequest: AvailabilityRequestFilterAPI;
  public displayName = '';
  public startTime = '';
  public endTime = '';

  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  constructor(
    public dialogRef: MatDialogRef<FormAvailabilityRequestComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogDataAvailabilityRequest) {
  }

  ngOnInit() {
    if (this.displayName === '') {
      this.displayName = this.data.displayName;
    }
    if (this.startTime === '') {
      this.startTime = this.data.startTime;
    }
    if (this.endTime === '') {
      this.endTime = this.data.endTime;
    }

    this.availabilityRequest = this.data.availabilityRequest;
    this.availabilityRequest.data.forEach(availability => {
      let availabilityImage = 'RoomBusy.ico';
      if (availability.status) {
        availabilityImage = 'RoomAvailable.ico';
      }
      this.recurringAvailability.push({ eventDate: new Date(availability.startTime).toDateString(), availabilityImage });
    });
    this.dataSource = new MatTableDataSource(this.recurringAvailability);
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  onNoClick(): void {
    this.dialogRef.close();
  }
}
