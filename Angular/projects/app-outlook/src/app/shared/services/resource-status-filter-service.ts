import { Injectable } from '@angular/core';
import { ResourceStatusFilterItem } from '../models/resource-room.model';

@Injectable()
export class ResourceStatusFilterService {
    public includeUnavailable = 'Include unavailable';
    public includeRestricted = 'Include restricted';

    private resourceStatusFilter: ResourceStatusFilterItem[] = [
        { value: this.includeUnavailable, checked: false },
        { value: this.includeRestricted, checked: false }
    ];

    getResourceStatusFilter() {
        return this.resourceStatusFilter.slice(); // copy
    }
}

