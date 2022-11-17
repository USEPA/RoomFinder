export class EquipmentAPI {
  id: number;
  displayName: string;
}

export interface EquipmentFilter {
  value: string;
  checked: boolean;
}

export interface Equipment {
  value: string;
  viewValue: string;
}


export class EquipmentFilterResults {
  samAccountName: string;
  emailAddress: string;
  availabilityImage: string;
  restrictionImage: string;
  restrictionTooltip: string;
  restrictionType: number;
  dependencies: string[];
  displayName: string;
  location: LocationAddress;
  equipmentType: string;
}

export class EquipmentDependency {
  emailAddress: string;
  displayName: string;
  equipmentType: string;
}

export class LocationAddress {
  country: string;
  state: string;
  city: string;
  office: string;
  floor: string;
}
