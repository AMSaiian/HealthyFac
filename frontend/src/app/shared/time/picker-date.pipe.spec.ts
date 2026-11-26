import { TestBed } from '@angular/core/testing';

import { LanguageStore } from '@shared/i18n/language.store';

import { PickerDatePipe } from './picker-date.pipe';
import { Temporal } from './temporal';

describe('PickerDatePipe', { tags: ['Platform'] }, () => {
  function createPipe(language: string): PickerDatePipe {
    TestBed.configureTestingModule({
      providers: [{ provide: LanguageStore, useValue: { language: () => language } }],
    });
    return TestBed.runInInjectionContext(() => new PickerDatePipe());
  }

  it.each([
    ['English', 'en', '11/19/2026'],
    ['Ukrainian', 'uk', '19.11.2026'],
  ])('transform_With%s_WritesTheLocalOrder', (_name, language, expected) => {
    const pipe = createPipe(language);

    const text = pipe.transform(Temporal.PlainDate.from('2026-11-19'));

    expect(text).toBe(expected);
  });

  it('transform_WithNull_ReturnsEmpty', () => {
    const pipe = createPipe('en');

    const text = pipe.transform(null);

    expect(text).toBe('');
  });
});
