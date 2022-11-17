import { DateFormatPipe } from './date-format.pipe';
import { DateFormatConstants } from './date-format-constants';

describe('DateFormatPipe', () => {
  it('create an instance', () => {
    const pipe = new DateFormatPipe('en-US');
    expect(pipe).toBeTruthy();
  });

  it('should convert to datetime format', () => {
    const pipe = new DateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('2018-06-13 15:35:50');
    const transformed = pipe.transform(dateTime);

    expect(transformed).toBe('06/13/2018 15:35');
  });

  it('should convert to date format', () => {
    const pipe = new DateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const dateTime = new Date('2018-06-13 15:35:50');
    const transformed = pipe.transform(dateTime, DateFormatConstants.dateFormat);

    expect(transformed).toBe('06/13/2018');
  });

  it('should handle null input', () => {
    const pipe = new DateFormatPipe('en-US');
    expect(pipe).toBeTruthy();

    const date = pipe.transform(null, DateFormatConstants.dateFormat, '-0400');

    expect(date).toBeNull();
  });
});
