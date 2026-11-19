import { TestBed } from '@angular/core/testing';

import { LanguageStore } from '@shared/i18n/language.store';

import { PickerDatePipe } from './picker-date.pipe';
import { Temporal } from './temporal';

describe('PickerDatePipe', () => {
  function createPipe(language: string): PickerDatePipe {
    TestBed.configureTestingModule({
      providers: [{ provide: LanguageStore, useValue: { language: () => language } }],
    });
    return TestBed.runInInjectionContext(() => new PickerDatePipe());
  }

  it('transform_WithEnglish_WritesMonthDayYear', () => {
    const pipe = createPipe('en');

    const text = pipe.transform(Temporal.PlainDate.from('2026-11-19'));

    expect(text).toBe('11/19/2026');
  });

  it('transform_WithUkrainian_WritesDayMonthYear', () => {
    const pipe = createPipe('uk');

    const text = pipe.transform(Temporal.PlainDate.from('2026-11-19'));

    expect(text).toBe('19.11.2026');
  });

  it('transform_WithNull_ReturnsEmpty', () => {
    const pipe = createPipe('en');

    const text = pipe.transform(null);

    expect(text).toBe('');
  });
});
