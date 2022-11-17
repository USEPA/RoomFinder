import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { ReportingService } from '../../../shared/services/reporting-service';
import { ResourcesCoreEntity } from '../../../shared/models/report.model';

@Component({
  selector: 'app-report-room-listing',
  templateUrl: './report-room-listing.component.html',
  styleUrls: []
})
export class ReportRoomListingComponent implements OnInit {
  @Output() roomsChange = new EventEmitter();
  rooms: ResourcesCoreEntity[] = [];
  selectRooms: any;

  constructor(private reportService: ReportingService) { }

  ngOnInit() {
    if (this.rooms.length === 0) {
      this.reportService.getRoomsFromAPI().subscribe((data) => {
        this.rooms = data;
      });
    }
  }

  roomsChanged() {
    this.roomsChange.emit(this.selectRooms);
  }
}
