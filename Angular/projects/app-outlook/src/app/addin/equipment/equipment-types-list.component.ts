import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { EquipmentService } from '../../shared/services/equipment-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-equipment-types-list',
  templateUrl: './equipment-types-list.component.html',
  styleUrls: []
})
export class EquipmentTypesListComponent implements OnInit {
  @Output() equipmentTypesChange = new EventEmitter();
  equipmentTypes: MatOptionItem[] = [];
  selectEquipmentTypes: any;

  constructor(private equipmentService: EquipmentService) { }

  ngOnInit() {
    if (this.equipmentTypes.length === 0) {
      this.equipmentService.getEquipmentTypesFromAPI().subscribe((data) => {
        this.equipmentTypes = this.getEquipmentTypes(data);
      });
    }
  }

  equipmentTypesChanged() {
    this.equipmentTypesChange.emit(this.selectEquipmentTypes);
  }

  getEquipmentTypes(equipmentTypesAPI: string[]) {
    if (this.equipmentTypes.length === 0) {
      equipmentTypesAPI.forEach(equipmentTypeName => {
        this.equipmentTypes.push({ value: equipmentTypeName, viewValue: equipmentTypeName });
      });
    }
    return this.equipmentTypes.slice(); // copy
  }
}
