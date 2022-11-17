export interface ResourcesCoreEntity {
  samAccountName: string;
  displayName: string;
  emailAddress: string;
}

export interface ResourcesEntity extends ResourcesCoreEntity {
  userPrincipalName: string;
  primarySmtpAddress: string;
  name: string;
  resourceCapacity: number;
  restrictionType?: string;
  bookingProcessing: string;
  city: string;
  company: string;
  countryOrRegion: string;
  department: string;
  office: string;
  phone: string;
  postalCode: string;
  stateOrProvince: string;
}

export interface MailboxesEntity extends ResourcesEntity {
  equipment?: string[] | null;
  restrictedDelegates?: (RestrictedDelegatesEntity | null)[] | null;
  dependencies?: (string | null)[] | null;
  equipmentDependencies?: EquipmentDependenciesEntity[] | null;
  samGUID: string;
  exchangeObjectId: string;
  guid: string;
  equipmentList?: null[] | null;
}

export interface RestrictedDelegatesEntity {
  userPrincipalName: string;
  userType: string;
}

export interface EquipmentDependenciesEntity {
  emailAddress: string;
  displayName: string;
  equipmentType: string;
}

export interface EquipmentsEntity extends ResourcesEntity {
  equipment?: null[] | null;
  equipmentList?: string[] | null;
  restrictedDelegates?: null[] | null;
  dependencies?: null[] | null;
  samGUID: string;
  exchangeObjectId: string;
  guid: string;
}

export interface LocationContext {
  equipments: EquipmentDependenciesEntity[];
  rooms: MailboxesEntity[];
}

export interface Resource {
  displayName: string;
  emailAddress: string;
}
