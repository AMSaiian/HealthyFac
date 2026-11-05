import { TestBed } from '@angular/core/testing';

import { LanguageStore } from '@shared/i18n/language.store';

import { CLOCK } from './clock';
import { LocalDateTimePipe } from './local-date-time.pipe';
import { Temporal } from './temporal';

describe('LocalDateTimePipe', () => {
  function createPipe(timeZoneId: string): LocalDateTimePipe {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => timeZoneId },
        },
        { provide: LanguageStore, useValue: { language: () => 'en' } },
      ],
    });
    return TestBed.runInInjectionContext(() => new LocalDateTimePipe());
  }

  it('transform_WithUtcInstant_ShowsTimeInBrowserZone', () => {
    const pipe = createPipe('Europe/Kyiv');

    const text = pipe.transform('2026-03-29T10:00:00Z');

    expect(text).toContain('13:00');
  });

  it('transform_WithNull_ReturnsEmptyText', () => {
    const pipe = createPipe('Europe/Kyiv');

    const text = pipe.transform(null);

    expect(text).toBe('');
  });
});
