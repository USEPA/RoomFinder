export class FilterBaseEntity {
    includeUnavailable: boolean;
    includeRestricted: boolean;
    start: string;
    end: string;
    capacity?: number;
    state?: string;
    city?: string;
    office?: string;
    floor?: string;
    listPath: string;
    requiredEquipment: any[];
    constructor(start: string, end: string, capacity: number = 0) {
        this.start = start;
        this.end = end;
        if (capacity > 0) {
            this.capacity = capacity;
        }
    }
}
