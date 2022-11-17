import { ZuluDateFormatPipe } from './zulu-date-format.pipe';
import { DateFormatConstants } from './date-format-constants';

describe('ZuluDateFormatPipe', () => {
  it('create an instance', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();
  });

  it('should convert to datetime format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('2018-06-13 15:35:50 GMT-4:00');
    const defaultDateTime = pipe.transform(dateTime, DateFormatConstants.dateTimeFormat, '-0400');

    expect(defaultDateTime).toContain('06/13/2018 11:35');
  });

  it('should convert to military datetime format from c# default format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('2018-06-13 15:35:50 GMT-4:00');
    const militaryDateTime = pipe.transform(dateTime, DateFormatConstants.militaryDateTimeFormat, '-0400');

    expect(militaryDateTime).toContain('13 JUN 2018 11:35');
  });

  it('should convert to military datetime format from c# zulu format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const zuluTime = '2018-06-29T23:07:26.9879938Z-0400';
    const militaryDateTime = pipe.transform(zuluTime, DateFormatConstants.militaryDateTimeFormat, '-0400');

    expect(militaryDateTime).toContain('29 JUN 2018 19:07');
  });

  it('should convert to military datetime format from c# format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const zuluTime = '2018-06-29T23:07:26.9879938-0400';
    const militaryDateTime = pipe.transform(zuluTime, DateFormatConstants.militaryDateTimeFormat, '-0400');

    expect(militaryDateTime).toContain('29 JUN 2018 19:07');
  });

  it('should convert to military datetime format from M/d/yyyy HH:mm format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('4/17/2018 17:14 GMT-4:00');
    const militaryDateTime = pipe.transform(dateTime, DateFormatConstants.militaryDateTimeFormat, '-0400');

    expect(militaryDateTime).toContain('17 APR 2018 13:14');
  });

  it('should convert to date format', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('2018-06-13 00:30:00');
    const defaultDate = pipe.transform(dateTime, DateFormatConstants.dateFormat, '-0400');

    expect(defaultDate).toContain('06/12/2018');
  });

  it('should handle null input', () => {
    const pipe = new ZuluDateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const defaultDate = pipe.transform(null, DateFormatConstants.dateFormat, '-0400');

    expect(defaultDate).toBeNull();
  });
});
