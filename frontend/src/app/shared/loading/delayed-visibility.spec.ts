import { TestScheduler } from 'rxjs/testing';

import { delayedVisibility } from '@shared/loading/delayed-visibility';

const SHOW_AFTER_MS = 300;
const MIN_VISIBLE_MS = 500;
const VALUES = { t: true, f: false };

function run(input: string, expected: string): void {
  const testScheduler = new TestScheduler((actual, expectedFrames) => {
    expect(actual).toEqual(expectedFrames);
  });

  testScheduler.run(({ cold, expectObservable }) => {
    const result$ = cold(input, VALUES).pipe(delayedVisibility(SHOW_AFTER_MS, MIN_VISIBLE_MS, testScheduler));

    expectObservable(result$).toBe(expected, VALUES);
  });
}

describe('delayedVisibility', () => {
  it('delayedVisibility_WithBusyShorterThanShowAfter_NeverShows', () => {
    run('t 99ms f', 'f');
  });

  it('delayedVisibility_WithBusyEndingRightAfterShow_StaysVisibleForMinimum', () => {
    run('t 300ms f', 'f 299ms t 499ms f');
  });

  it('delayedVisibility_WithLongBusy_StaysVisibleUntilItEnds', () => {
    run('t 1000ms f', 'f 299ms t 700ms f');
  });

  it('delayedVisibility_WithBusyAgainWhileVisible_DoesNotBlink', () => {
    run('t 300ms f 98ms t 599ms f', 'f 299ms t 699ms f');
  });
});
