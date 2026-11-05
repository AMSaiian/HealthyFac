import { inject, Pipe, type PipeTransform } from '@angular/core';

import { LanguageStore } from '@shared/i18n/language.store';

import { CLOCK } from './clock';
import { Temporal } from './temporal';

/**
 * Shows a UTC instant (an ISO string from the API) in the time zone of the browser. The pipe is impure, because the
 * language and the zone are read at run time.
 */
@Pipe({
  name: 'localDateTime',
  pure: false,
})
export class LocalDateTimePipe implements PipeTransform {
  private readonly _clock = inject(CLOCK);
  private readonly _language = inject(LanguageStore);

  transform(value: string | null): string {
    if (value === null) {
      return '';
    }
    return Temporal.Instant.from(value)
      .toZonedDateTimeISO(this._clock.timeZoneId())
      .toLocaleString(this._language.language(), { dateStyle: 'medium', timeStyle: 'short', hourCycle: 'h23' });
  }
}
