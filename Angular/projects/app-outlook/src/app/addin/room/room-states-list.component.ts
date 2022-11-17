import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { MatOptionItem } from '../../shared/models/matoption.model';
import { RoomService } from '../../shared/services/room-service';

@Component({
  selector: 'app-room-states-list',
  templateUrl: './room-states-list.component.html',
  styleUrls: []
})
export class RoomStatesListComponent implements OnInit {
  @Output() statesChange = new EventEmitter();
  states: MatOptionItem[] = [];
  selectStates: any;

  constructor(private roomService: RoomService) { }

  ngOnInit() {
    if (this.states.length === 0) {
      this.roomService.getStatesFromAPI().subscribe((data) => {
        this.states = this.getStates(data);
      });
    }
  }

  statesChanged() {
    this.statesChange.emit(this.selectStates);
  }

  getStates(statesAPI: string[]) {
    if (this.states.length === 0) {
      statesAPI.forEach(element => {
        this.states.push({ value: element, viewValue: element });
      });
    }
    return this.states.slice(); // copy
  }
}
