import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-equipment-states-list',
  templateUrl: './equipment-states-list.component.html',
  styleUrls: []
})
export class EquipmentStatesListComponent implements OnInit {
  @Output() statesChange = new EventEmitter();
  states: MatOptionItem[] = [];
  selectStates: any;

  constructor(public equipmentService: EquipmentService) { }

  ngOnInit() {
    if (this.states.length === 0) {
      this.equipmentService.getStatesFromAPI().subscribe((data) => {
        this.states = this.getStates(data);
      });
    }
  }

  statesChanged() {
    this.statesChange.emit(this.selectStates);
  }

  getStates(statesAPI: string[]) {
    if (this.states.length === 0) {
      statesAPI.forEach(element => {
        this.states.push({ value: element, viewValue: element });
      });
    }
    return this.states.slice(); // copy
  }
}
