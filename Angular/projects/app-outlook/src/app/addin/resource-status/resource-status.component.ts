import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { ResourceStatusFilterService } from '../../shared/services/resource-status-filter-service';
import { ResourceStatusFilterItem } from '../../shared/models/resource-room.model';

@Component({
  selector: 'app-resource-status',
  templateUrl: './resource-status.component.html',
  styleUrls: []
})
export class ResourceStatusComponent implements OnInit {
  @Output() resourceStatusChange = new EventEmitter();
  resourceStatusFilter: ResourceStatusFilterItem[];

  constructor(private resourceStatusFilterService: ResourceStatusFilterService) { }

  ngOnInit() {
    this.resourceStatusFilter = this.resourceStatusFilterService.getResourceStatusFilter();
  }

  resourceStatusChanged() {
    this.resourceStatusChange.emit(this.resourceStatusFilter);
  }
}
