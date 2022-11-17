import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { MatOptionItem } from '../../shared/models/matoption.model';
import { RoomService } from '../../shared/services/room-service';

@Component({
  selector: 'app-room-offices-list',
  templateUrl: './room-offices-list.component.html',
  styleUrls: []
})
export class RoomOfficesListComponent implements OnInit {
  @Output() officesChange = new EventEmitter();
  offices: MatOptionItem[] = [];
  selectOffices: any;

  constructor(private roomService: RoomService) { }

  ngOnInit() {
  }

  officesChanged() {
    this.officesChange.emit(this.selectOffices);
  }

  public refreshOffices(state: string, city: string) {
    if ((state !== '') && (city !== '')) {
      this.roomService.getOfficesFromAPI(state, city).subscribe(data => {
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
    return this.offices.slice();
  }
}
