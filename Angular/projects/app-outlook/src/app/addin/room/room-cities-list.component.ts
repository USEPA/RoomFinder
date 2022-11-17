import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { RoomService } from '../../shared/services/room-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-room-cities-list',
  templateUrl: './room-cities-list.component.html',
  styleUrls: []
})
export class RoomCitiesListComponent implements OnInit {
  @Output() citiesChange = new EventEmitter();
  cities: MatOptionItem[] = [];
  selectCities: any;

  constructor(private roomService: RoomService) { }

  ngOnInit() {
  }

  citiesChanged() {
    this.citiesChange.emit(this.selectCities);
  }

  public refreshCities(state: string) {
    if (state !== '') {
      this.roomService.getCitiesFromAPI(state).subscribe(data => {
        this.cities = this.getCities(data);
      });
    }
  }

  getCities(citiesAPI: string[]) {
    if (this.cities.length === 0) {
      citiesAPI.forEach(element => {
        this.cities.push({ value: element, viewValue: element });
      });
    }
    return this.cities.slice();
  }
}
