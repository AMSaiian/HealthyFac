import type { Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, timer } from 'rxjs';

import type { Clock } from '@shared/time/clock';
import type { Temporal } from '@shared/time/temporal';

const MINUTE_MS = 60_000;

export function todayOf(clock: Clock): Temporal.PlainDate {
  return clock.now().toZonedDateTimeISO(clock.timeZoneId()).toPlainDate();
}

/**
 * A signal of the current instant that ticks on each full minute, so the "now" line moves on the minute. The first
 * tick comes after the rest of the current minute. Call it in an injection context.
 */
export function minuteTicker(clock: Clock): Signal<Temporal.Instant> {
  const restOfMinute = MINUTE_MS - (clock.now().epochMilliseconds % MINUTE_MS);
  return toSignal(timer(restOfMinute, MINUTE_MS).pipe(map(() => clock.now())), { initialValue: clock.now() });
}
