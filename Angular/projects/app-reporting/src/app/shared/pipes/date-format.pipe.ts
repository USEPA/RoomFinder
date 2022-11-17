import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateFormatConstants } from './date-format-constants';

@Pipe({
  name: 'dateFormat'
})
export class DateFormatPipe extends DatePipe implements PipeTransform {
  transform(value: any, format = DateFormatConstants.dateTimeFormat, timezone?: string, locale?: string): string|null {
    if (!value) {
      return null;
    }

    return super.transform(value, format, timezone, locale).toUpperCase();
  }
}
