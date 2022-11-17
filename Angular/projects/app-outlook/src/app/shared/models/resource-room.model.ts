import { FilterBaseEntity } from './filter-base.model';
import { EquipmentDependency } from './resource-equipment.model';

export class RoomListing {
  emailAddress: string;
  displayName: string;
}

export interface ResourceStatusFilterItem {
  value: string;
  checked: boolean;
}

export class RoomFilterEntity extends FilterBaseEntity {
  capacity: number;
  constructor(start: string, end: string, capacity: number = 0) {
    super(start, end);
    this.capacity = capacity;
  }
}

export class RoomFilterResults {
  samAccountName: string;
  capacity: number;
  emailAddress: string;
  availabilityImage: string;
  restrictionImage: string;
  restrictionTooltip: string;
  restrictionType: number;
  dependencies?: any[];
  equipmentDependencies?: EquipmentDependency[];
  displayName: string;
}
