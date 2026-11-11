import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { delayedLoading } from './delayed-loading';

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

  it('delayedLoading_WhenTheLoadEndsBefore300ms_NeverTurnsOn', () => {
    const { isLoading, shown } = setup();

    isLoading.set(true);
    TestBed.tick();
    vi.advanceTimersByTime(299);
    isLoading.set(false);
    TestBed.tick();
    vi.advanceTimersByTime(1000);

    expect(shown()).toBe(false);
  });

  it('delayedLoading_WhenTheLoadTakesLonger_TurnsOnAfter300msAndStaysAtLeast500ms', () => {
    const { isLoading, shown } = setup();

    isLoading.set(true);
    TestBed.tick();
    vi.advanceTimersByTime(300);
    const isShownAfterDelay = shown();
    vi.advanceTimersByTime(50);
    isLoading.set(false);
    TestBed.tick();
    vi.advanceTimersByTime(449);
    const isShownBeforeMinimum = shown();
    vi.advanceTimersByTime(1);

    expect(isShownAfterDelay).toBe(true);
    expect(isShownBeforeMinimum).toBe(true);
    expect(shown()).toBe(false);
  });
});
