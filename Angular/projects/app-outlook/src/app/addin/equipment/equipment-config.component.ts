import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { EquipmentAPI, EquipmentFilter } from '../../shared/models/resource-equipment.model';
import { EquipmentService } from '../../shared/services/equipment-service';

@Component({
  selector: 'app-equipment-config',
  templateUrl: './equipment-config.component.html',
  styleUrls: []
})
export class EquipmentConfigComponent implements OnInit {
  @Output() equipmentConfigChange = new EventEmitter();
  equipmentFilter: EquipmentFilter[] = [];
  equipments: EquipmentAPI[] = [];

  constructor(public equipmentService: EquipmentService) { }

  ngOnInit() {
    this.equipmentService.getEquipmentFromAPI().subscribe((data) => {
      this.equipments = data;
      this.equipmentFilter = this.getEquipmentFilter(data);
    });
  }

  equipmentConfigChanged() {
    this.equipmentConfigChange.emit(this.equipmentFilter);
  }

  getEquipmentFilter(equipment: EquipmentAPI[]) {
    if (this.equipmentFilter.length === 0) {
      equipment.forEach(element => {
        this.equipmentFilter.push({ value: element.displayName, checked: false });
      });
    }
    return this.equipmentFilter.slice(); // copy
  }
}
