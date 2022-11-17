import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { RoomService } from '../../shared/services/room-service';
import { LocationsAPI } from '../../shared/models/locations.model';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-location-list',
  templateUrl: './location-list.component.html',
  styleUrls: ['./location-list.component.scss']
})
export class LocationListComponent implements OnInit {
  @Output() locationChange = new EventEmitter();
  locations: MatOptionItem[] = [];
  selectLocation: any;

  constructor(private roomService: RoomService) { }

  ngOnInit() {
    if (this.locations.length === 0) {
      this.roomService.getLocationsFromAPI().subscribe((data) => {
        this.locations = this.getLocations(data);
      });
    }
  }

  locationChanged() {
    this.locationChange.emit(this.selectLocation);
  }


  getLocations(locationsAPI: LocationsAPI[]) {
    if (this.locations.length === 0) {
      locationsAPI.forEach(element => {
        this.locations.push({ value: element.displayName, viewValue: element.displayName });
      });
    }
    return this.locations.slice(); // copy
  }
}
