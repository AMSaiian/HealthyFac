import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import type { Clock } from './clock';
import { minuteTicker, todayOf } from './clock-ticks';
import { Temporal } from './temporal';

describe('clock-ticks', { tags: ['Platform'] }, () => {
  let currentNow = Temporal.Instant.from('2027-06-01T20:59:30Z');
  const clock: Clock = { now: () => currentNow, timeZoneId: () => 'Europe/Kyiv' };

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'setInterval', 'clearInterval', 'clearTimeout'] });
    currentNow = Temporal.Instant.from('2027-06-01T20:59:30Z');
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('todayOf_AfterMidnightInTheClockZone_GivesTheNextDate', () => {
    const before = todayOf(clock).toString();
    currentNow = Temporal.Instant.from('2027-06-01T21:00:00Z');

    expect(before).toBe('2027-06-01');
    expect(todayOf(clock).toString()).toBe('2027-06-02');
  });

  it('minuteTicker_FirstTick_ComesAtTheNextFullMinute', async () => {
    const ticker = TestBed.runInInjectionContext(() => minuteTicker(clock));
    currentNow = Temporal.Instant.from('2027-06-01T21:00:00Z');

    await vi.advanceTimersByTimeAsync(29_999);
    const beforeTick = ticker().toString();
    await vi.advanceTimersByTimeAsync(1);
    TestBed.inject(ApplicationRef).tick();

    expect(beforeTick).toBe('2027-06-01T20:59:30Z');
    expect(ticker().toString()).toBe('2027-06-01T21:00:00Z');
  });
});
