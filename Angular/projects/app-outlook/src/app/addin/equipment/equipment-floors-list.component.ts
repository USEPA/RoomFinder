import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-equipment-floors-list',
  templateUrl: './equipment-floors-list.component.html',
  styleUrls: []
})
export class EquipmentFloorsListComponent implements OnInit {
  @Output() floorsChange = new EventEmitter();
  floors: MatOptionItem[] = [];
  selectFloors: any;

  constructor(public equipmentService: EquipmentService) { }

  ngOnInit() {
  }

  floorsChanged() {
    this.floorsChange.emit(this.selectFloors);
  }

  public refreshFloors(state: string, city: string, office: string) {
    if ((state !== '') && (city !== '') && (office !== '')) {
      this.equipmentService.getFloorsFromAPI(state, city, office).subscribe(data => {
        this.floors = this.getFloors(data);
      });
    }
  }

  getFloors(floorsAPI: string[]) {
    if (this.floors.length === 0) {
      floorsAPI.forEach(element => {
        this.floors.push({ value: element, viewValue: element });
      });
    }
    return this.floors.slice(); // copy
  }
}
