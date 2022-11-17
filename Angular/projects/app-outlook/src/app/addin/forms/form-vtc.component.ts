import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { VTCDialogData } from '../../shared/models/vtc.model';
import { Checkboxes } from '../../shared/models/matcheckbox.model';

@Component({
  selector: 'app-form-vtc',
  templateUrl: './form-vtc.component.html',
  styleUrls: ['./form-vtc.component.scss']
})
export class FormVtcComponent implements OnInit {
  array: Checkboxes[] = [
    { name: 'Teams', checked: false },
    { name: 'Skype for Business', checked: false },
    { name: 'Office 365 Conference', checked: false },
    { name: 'VTC Point to Point', checked: false },
    { name: 'VTC Multipoint', checked: false }
  ];

  constructor(
    public dialogRef: MatDialogRef<FormVtcComponent>,
    @Inject(MAT_DIALOG_DATA) public vtcData: VTCDialogData) { }

  ngOnInit() {
    this.vtcData.conferenceTypes = this.array;
  }

  onNoClick(): void {
    this.dialogRef.close();
  }

  getCheckboxes(): Checkboxes[] {
    return this.array;
  }
}
