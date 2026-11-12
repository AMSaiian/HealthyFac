import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { delayedLoading } from './delayed-loading';
import { SPINNER_MIN_VISIBLE_MS, SPINNER_SHOW_AFTER_MS } from './loading-constants';

const LOAD_AFTER_SHOWN_MS = 50;

describe('delayedLoading', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function setup(): { isLoading: ReturnType<typeof signal<boolean>>; shown: () => boolean } {
    const isLoading = signal(false);
    const shown = TestBed.runInInjectionContext(() => delayedLoading(isLoading));
    TestBed.tick();
    return { isLoading, shown };
  }

  it('delayedLoading_WhenTheLoadEndsBeforeTheShowDelay_NeverTurnsOn', () => {
    const { isLoading, shown } = setup();

    isLoading.set(true);
    TestBed.tick();
    vi.advanceTimersByTime(SPINNER_SHOW_AFTER_MS - 1);
    isLoading.set(false);
    TestBed.tick();
    vi.advanceTimersByTime(1000);

    expect(shown()).toBe(false);
  });

  it('delayedLoading_WhenTheLoadTakesLonger_TurnsOnAfterTheShowDelayAndStaysTheMinimumTime', () => {
    const { isLoading, shown } = setup();

    isLoading.set(true);
    TestBed.tick();
    vi.advanceTimersByTime(SPINNER_SHOW_AFTER_MS);
    const isShownAfterDelay = shown();
    vi.advanceTimersByTime(LOAD_AFTER_SHOWN_MS);
    isLoading.set(false);
    TestBed.tick();
    vi.advanceTimersByTime(SPINNER_MIN_VISIBLE_MS - LOAD_AFTER_SHOWN_MS - 1);
    const isShownBeforeMinimum = shown();
    vi.advanceTimersByTime(1);

    expect(isShownAfterDelay).toBe(true);
    expect(isShownBeforeMinimum).toBe(true);
    expect(shown()).toBe(false);
  });
});
