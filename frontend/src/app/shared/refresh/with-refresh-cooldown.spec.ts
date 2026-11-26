import { TestBed } from '@angular/core/testing';
import { signalStore, withMethods } from '@ngrx/signals';

import { REFRESH_COOLDOWN_SECONDS, withRefreshCooldown } from '@shared/refresh/with-refresh-cooldown';

const CooldownStore = signalStore(
  withRefreshCooldown(),
  withMethods((store) => ({
    press(): boolean {
      if (store.isRefreshCoolingDown()) {
        return false;
      }
      store.startRefreshCooldown();
      return true;
    },
  })),
);

describe('withRefreshCooldown', { tags: ['Platform'] }, () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('startRefreshCooldown_AfterAClick_KeepsTheButtonOffUntilTheSecondsRunOut', () => {
    const store = TestBed.configureTestingModule({ providers: [CooldownStore] }).inject(CooldownStore);

    const first = store.press();
    vi.advanceTimersByTime((REFRESH_COOLDOWN_SECONDS - 1) * 1000);
    const duringTheWait = store.press();
    const secondsLeft = store.refreshSecondsLeft();
    vi.advanceTimersByTime(1000);
    const afterTheWait = store.press();

    expect(first).toBe(true);
    expect(duringTheWait).toBe(false);
    expect(secondsLeft).toBe(1);
    expect(afterTheWait).toBe(true);
  });
});
