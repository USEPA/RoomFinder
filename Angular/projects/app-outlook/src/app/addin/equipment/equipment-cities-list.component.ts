import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-equipment-cities-list',
  templateUrl: './equipment-cities-list.component.html',
  styleUrls: []
})
export class EquipmentCitiesListComponent implements OnInit {
  @Output() citiesChange = new EventEmitter();
  cities: MatOptionItem[] = [];
  selectCities: any;

  constructor(public citiesListService: EquipmentService) { }

  ngOnInit() {
  }

  citiesChanged() {
    this.citiesChange.emit(this.selectCities);
  }

  public refreshCities(state: string) {
    if (state !== '') {
      this.citiesListService.getCitiesFromAPI(state).subscribe(data => {
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
    return this.cities.slice(); // copy
  }
}
