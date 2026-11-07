import {
  asyncScheduler,
  defer,
  distinctUntilChanged,
  EMPTY,
  map,
  of,
  type OperatorFunction,
  type SchedulerLike,
  startWith,
  switchMap,
  tap,
  timer,
} from 'rxjs';

/**
 * Turns a busy flag into a visibility flag that never flashes. A busy phase shorter than `showAfterMs` never shows.
 * Once shown, the flag stays visible for at least `minVisibleMs`. A new busy phase while it is visible keeps it
 * visible without a blink.
 *
 * @param showAfterMs How long the busy flag must stay true before the result turns true.
 * @param minVisibleMs How long the result stays true at least, once it turned true.
 * @param scheduler The scheduler of the timers. Tests pass a `TestScheduler`.
 */
export function delayedVisibility(
  showAfterMs: number,
  minVisibleMs: number,
  scheduler: SchedulerLike = asyncScheduler,
): OperatorFunction<boolean, boolean> {
  return (isLoading$) =>
    defer(() => {
      let shownAt: number | null = null;

      return isLoading$.pipe(
        distinctUntilChanged(),
        switchMap((isLoading) => {
          if (isLoading) {
            if (shownAt !== null) {
              return EMPTY;
            }

            return timer(showAfterMs, scheduler).pipe(
              tap(() => (shownAt = scheduler.now())),
              map(() => true),
            );
          }

          if (shownAt === null) {
            return of(false);
          }

          const remainingMs = Math.max(0, minVisibleMs - (scheduler.now() - shownAt));

          return timer(remainingMs, scheduler).pipe(
            tap(() => (shownAt = null)),
            map(() => false),
          );
        }),
        startWith(false),
        distinctUntilChanged(),
      );
    });
}
