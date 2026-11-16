import { TestBed } from '@angular/core/testing';

import { LanguageStore } from '@shared/i18n/language.store';

import { PlainDatePipe } from './plain-date.pipe';

describe('PlainDatePipe', () => {
  function createPipe(): PlainDatePipe {
    TestBed.configureTestingModule({
      providers: [{ provide: LanguageStore, useValue: { language: () => 'en' } }],
    });
    return TestBed.runInInjectionContext(() => new PlainDatePipe());
  }

  it('transform_WithPlainDay_ShowsTheSameDayWithoutZoneShift', () => {
    const pipe = createPipe();

    const text = pipe.transform('2099-01-01');

    expect(text).toBe('Jan 1, 2099');
  });

  it('transform_WithNull_ReturnsEmptyText', () => {
    const pipe = createPipe();

    const text = pipe.transform(null);

    expect(text).toBe('');
  });
});
