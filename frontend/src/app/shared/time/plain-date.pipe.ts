import { inject, Pipe, type PipeTransform } from '@angular/core';

import { LanguageStore } from '@shared/i18n/language.store';

import { Temporal } from './temporal';

/**
 * Shows a plain day (`yyyy-MM-dd` from the API) in the language of the app. A day has no time zone, so the pipe never
 * converts it. The pipe is impure, because the language is read at run time.
 */
@Pipe({
  name: 'plainDate',
  pure: false,
})
export class PlainDatePipe implements PipeTransform {
  private readonly _language = inject(LanguageStore);

  transform(value: string | null): string {
    if (value === null) {
      return '';
    }
    return Temporal.PlainDate.from(value).toLocaleString(this._language.language(), { dateStyle: 'medium' });
  }
}
