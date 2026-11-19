import { inject, Pipe, type PipeTransform } from '@angular/core';

import { LanguageStore } from '@shared/i18n/language.store';

import type { Temporal } from './temporal';

/**
 * Shows a plain day in the format of the date picker (`PRIMENG.dateFormat`: `mm/dd/yy` in English, `dd.mm.yy` in
 * Ukrainian), so a read-only view shows the same text as the picker input. The pipe is impure, because the language is
 * read at run time.
 */
@Pipe({
  name: 'pickerDate',
  pure: false,
})
export class PickerDatePipe implements PipeTransform {
  private readonly _language = inject(LanguageStore);

  transform(value: Temporal.PlainDate | null): string {
    if (value === null) {
      return '';
    }
    return value.toLocaleString(this._language.language(), { year: 'numeric', month: '2-digit', day: '2-digit' });
  }
}
