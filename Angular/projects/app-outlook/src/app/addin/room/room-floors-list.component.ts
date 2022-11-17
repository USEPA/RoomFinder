import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { RoomService } from '../../shared/services/room-service';
import { MatOptionItem } from '../../shared/models/matoption.model';

@Component({
  selector: 'app-room-floors-list',
  templateUrl: './room-floors-list.component.html',
  styleUrls: []
})
export class RoomFloorsListComponent implements OnInit {
  @Output() floorsChange = new EventEmitter();
  floors: MatOptionItem[] = [];
  selectFloors: any;

  constructor(private roomService: RoomService) { }

  ngOnInit() {
  }

  floorsChanged() {
    this.floorsChange.emit(this.selectFloors);
  }

  public refreshFloors(state: string, city: string, office: string) {
    if ((state !== '') && (city !== '') && (office !== '')) {
      this.roomService.getFloorsFromAPI(state, city, office).subscribe(data => {
        this.floors = this.getFloors(data);
      });
    }
  }

  getFloors(floorsAPI: string[]) {
    if (this.floors.length === 0) {
      floorsAPI.forEach(element => {
        this.floors.push({ value: element, viewValue: element });
      });
    }
    return this.floors.slice(); // copy
  }
}
