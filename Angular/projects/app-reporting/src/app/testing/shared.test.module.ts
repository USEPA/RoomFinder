import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  imports: [
    SharedModule,
    NoopAnimationsModule,
  ],
  exports: [
    SharedModule,
    NoopAnimationsModule,
  ],
  providers: [
  ]

})
export class SharedTestModule { }
