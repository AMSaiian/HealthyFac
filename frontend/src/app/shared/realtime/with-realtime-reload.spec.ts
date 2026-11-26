import { TestBed } from '@angular/core/testing';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { Subject } from 'rxjs';
import type { Mock } from 'vitest';

import { type RealtimeHint, RealtimeTopic } from './realtime';
import { RealtimeConnection } from './realtime-connection';
import { withRealtimeReload } from './with-realtime-reload';

describe('withRealtimeReload', { tags: ['Realtime'] }, () => {
  let hints$: Subject<RealtimeHint>;
  let reconnected$: Subject<void>;
  let reload: Mock<() => void>;

  function createStore(topics: readonly RealtimeTopic[], scope?: boolean): { setScope(value: string | null): void } {
    const Store = signalStore(
      withState({ scope: 'case-1' as string | null }),
      withMethods((store) => ({
        setScope(value: string | null): void {
          patchState(store, { scope: value });
        },
      })),
      withRealtimeReload<{ scope: () => string | null }>(
        topics,
        () => reload(),
        scope === true ? (store) => store.scope() : undefined,
      ),
    );
    TestBed.configureTestingModule({
      providers: [{ provide: RealtimeConnection, useValue: { hints$, reconnected$ } }, Store],
    });
    return TestBed.inject(Store);
  }

  beforeEach(() => {
    vi.useFakeTimers();
    hints$ = new Subject<RealtimeHint>();
    reconnected$ = new Subject<void>();
    reload = vi.fn();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('hint_ThreeMatchingHintsWithinOneSecond_ReloadsOnceOneSecondAfterTheLast', async () => {
    createStore([RealtimeTopic.WorkOrders]);

    hints$.next({ topic: RealtimeTopic.WorkOrders });
    await vi.advanceTimersByTimeAsync(400);
    hints$.next({ topic: RealtimeTopic.WorkOrders });
    await vi.advanceTimersByTimeAsync(400);
    hints$.next({ topic: RealtimeTopic.WorkOrders });
    await vi.advanceTimersByTimeAsync(999);
    expect(reload).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(1);

    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('hint_OfAnotherTopic_DoesNotReload', async () => {
    createStore([RealtimeTopic.WorkOrders]);

    hints$.next({ topic: RealtimeTopic.Machines });
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).not.toHaveBeenCalled();
  });

  it('hint_OfOneOfSeveralTopics_Reloads', async () => {
    createStore([RealtimeTopic.WorkOrders, RealtimeTopic.Schedule]);

    hints$.next({ topic: RealtimeTopic.Schedule });
    await vi.advanceTimersByTimeAsync(1000);

    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('reconnected_AfterAReconnect_ReloadsOnce', async () => {
    createStore([RealtimeTopic.WorkOrders]);

    reconnected$.next();
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).toHaveBeenCalledTimes(1);
  });

  it.each([
    ['TheScopeOfTheStore', RealtimeTopic.Cases, { caseId: 'case-1' }],
    ['TheWorkOrderIdOfTheStore', RealtimeTopic.WorkOrders, { workOrderId: 'case-1' }],
  ])('hint_With%s_Reloads', async (_name, topic, ids) => {
    createStore([topic], true);

    hints$.next({ topic, ...ids });
    await vi.advanceTimersByTimeAsync(1000);

    expect(reload).toHaveBeenCalledTimes(1);
  });

  it.each([{ caseId: 'case-2' }, {}])('hint_WithAnotherScopeOrNone_DoesNotReload', async (ids) => {
    createStore([RealtimeTopic.Cases], true);

    hints$.next({ topic: RealtimeTopic.Cases, ...ids });
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).not.toHaveBeenCalled();
  });

  it('hint_WithAnotherWorkOrderId_DoesNotReload', async () => {
    createStore([RealtimeTopic.WorkOrders], true);

    hints$.next({ topic: RealtimeTopic.WorkOrders, workOrderId: 'other' });
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).not.toHaveBeenCalled();
  });

  it('hint_WhenTheStoreShowsNoScope_DoesNotReload', async () => {
    const store = createStore([RealtimeTopic.Cases], true);
    store.setScope(null);

    hints$.next({ topic: RealtimeTopic.Cases, caseId: 'case-1' });
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).not.toHaveBeenCalled();
  });

  it('hint_AfterTheStoreIsDestroyed_DoesNotReload', async () => {
    createStore([RealtimeTopic.WorkOrders]);
    TestBed.resetTestingModule();

    hints$.next({ topic: RealtimeTopic.WorkOrders });
    await vi.advanceTimersByTimeAsync(5000);

    expect(reload).not.toHaveBeenCalled();
  });
});
