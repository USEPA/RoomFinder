import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-equipment-offices-list',
  templateUrl: './equipment-offices-list.component.html',
  styleUrls: []
})
export class EquipmentOfficesListComponent implements OnInit {
  @Output() officesChange = new EventEmitter();
  offices: MatOptionItem[] = [];
  selectOffices: any;

  constructor(public equipmentService: EquipmentService) { }

  ngOnInit() {
  }

  officesChanged() {
    this.officesChange.emit(this.selectOffices);
  }

  public refreshOffices(state: string, city: string) {
    if ((state !== '') && (city !== '')) {
      this.equipmentService.getOfficesFromAPI(state, city).subscribe(data => {
        this.offices = this.getOffices(data);
      });
    }
  }

  getOffices(officesAPI: string[]) {
    if (this.offices.length === 0) {
      officesAPI.forEach(element => {
        this.offices.push({ value: element, viewValue: element });
      });
    }
    return this.offices.slice(); // copy
  }
}
