import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { Event } from '../../../shared/models/event.model';
import { AuthService } from '../../../shared/services/auth-service';
import { DateFormatConstants } from '../../../shared/pipes/date-format-constants';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-my-calendar',
  templateUrl: './my-calendar.component.html',
  styleUrls: ['./my-calendar.component.scss']
})
export class MyCalendarComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  public events: Event[] = [];
  public militaryDateTimeFormat = DateFormatConstants.militaryDateTimeFormat;
  public displayedColumns: string[] = ['Organizer',
    'subject',
    'Start',
    'End'
  ];
  public dataSource = new MatTableDataSource<Event>([]);

  constructor(
    private graphService: AuthService
  ) { }

  ngOnInit() {
    this.dataSource = new MatTableDataSource(this.events);
    this.graphService.getEvents().subscribe((events) => {
      this.events = events;
      this.dataSource = new MatTableDataSource<Event>(events);
      this.dataSource.sort = this.sort;
      this.dataSource.paginator = this.paginator;
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyFilter(filterValue: string): void {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.dataSource.filter = filterValue;

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  onSortChange(e: Sort) {
    console.log(e);
  }

}
