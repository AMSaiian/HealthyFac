import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';

import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';
import { REALTIME_RELOAD_DEBOUNCE_MILLISECONDS, withRealtimeReload } from '@shared/realtime/with-realtime-reload';
import { REFRESH_COOLDOWN_SECONDS } from '@shared/refresh/with-refresh-cooldown';

import { withGridRefresh } from './with-grid-refresh';

describe('withGridRefresh', { tags: ['Platform'] }, () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function createStore(reloadSpy: () => void) {
    const Store = signalStore(withGridRefresh(() => reloadSpy()));
    return TestBed.configureTestingModule({ providers: [Store] }).inject(Store);
  }

  it('refresh_WhenIdle_ReloadsOnceAndStartsTheCooldown', () => {
    const reloadSpy = vi.fn();
    const store = createStore(reloadSpy);

    store.refresh();

    expect(reloadSpy).toHaveBeenCalledTimes(1);
    expect(store.refreshSecondsLeft()).toBe(REFRESH_COOLDOWN_SECONDS);
  });

  it('refresh_DuringTheCooldown_DoesNotReload', () => {
    const reloadSpy = vi.fn();
    const store = createStore(reloadSpy);

    store.refresh();
    store.refresh();

    expect(reloadSpy).toHaveBeenCalledTimes(1);
  });

  it('refresh_AfterTheCooldown_ReloadsAgain', () => {
    const reloadSpy = vi.fn();
    const store = createStore(reloadSpy);

    store.refresh();
    vi.advanceTimersByTime(REFRESH_COOLDOWN_SECONDS * 1000);
    store.refresh();

    expect(reloadSpy).toHaveBeenCalledTimes(2);
  });

  it('realtimeHint_WithGridRefresh_DoesNotStartTheCooldown', () => {
    const reloadSpy = vi.fn();
    const fake = createFakeRealtimeConnection();
    const Store = signalStore(
      withGridRefresh(() => reloadSpy()),
      withRealtimeReload([RealtimeTopic.Machines], () => reloadSpy()),
    );
    const store = TestBed.configureTestingModule({
      providers: [Store, provideFakeRealtimeConnection(fake)],
    }).inject(Store);

    fake.hints$.next({ topic: RealtimeTopic.Machines });
    vi.advanceTimersByTime(REALTIME_RELOAD_DEBOUNCE_MILLISECONDS);

    expect(reloadSpy).toHaveBeenCalledTimes(1);
    expect(store.refreshSecondsLeft()).toBe(0);
  });
});
