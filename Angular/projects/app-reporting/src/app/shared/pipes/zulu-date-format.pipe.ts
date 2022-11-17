import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateFormatConstants } from './date-format-constants';

@Pipe({
  name: 'zuluDateFormat'
})
export class ZuluDateFormatPipe extends DatePipe implements PipeTransform {
  private timeZone: string;

  transform(value: any, format = DateFormatConstants.dateTimeFormat, timezone?: string, locale?: string): string|null {
    if (!value) {
      return null;
    }

    // remove 'Z' if its already in zulu format
    if (typeof value === 'string') {
      value = value.replace('Z', '');
    }

    // ensures time is in the expected format in order to format to zulu time.
    const normalized = super.transform(value, DateFormatConstants.dateTimeFormat, timezone, locale);

    // convert to local time zone by adding zulu tag to normilized date.
    const zulu = new Date(`${normalized}Z`);

    return super.transform(zulu, format, timezone, locale).toUpperCase() + ` ${this.getTimeZone()}`;
  }

  private getTimeZone(): string {
    if (this.timeZone === undefined) {
      const date = new Date();
      // get time zone in parenthesis from '23:15:30 GMT-0400 (Eastern Daylight Time)'
      const longNameTimeZone = (date.toTimeString().match(/\((.*)\)/)).pop();
      // remove empty lower case letters from 'Eastern Daylight Time'
      const shortNameTimeZone = longNameTimeZone.replace(/[a-z]/g, '');
      // remove white spaces from 'E D T'
      this.timeZone = shortNameTimeZone.replace(/\s/g, '');
    }

    return this.timeZone;
  }
}




