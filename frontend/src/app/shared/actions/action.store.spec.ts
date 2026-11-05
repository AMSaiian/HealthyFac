import { TestBed } from '@angular/core/testing';
import { z } from 'zod/mini';

import { provideActionHosts } from './action-registry';
import { ActionStore } from './action.store';
import type { ActionHostRegistration } from './app-action';

const CREATE_TYPE = 'things.create';
const SAVED_TYPE = 'things.saved';

const registration: ActionHostRegistration = {
  type: CREATE_TYPE,
  load: () => Promise.resolve({ component: class TestHost {}, schema: z.object({ name: z.string() }) }),
};

describe('ActionStore', () => {
  function createStore(): InstanceType<typeof ActionStore> {
    TestBed.configureTestingModule({ providers: [provideActionHosts([registration])] });
    return TestBed.inject(ActionStore);
  }

  it('dispatch_WithRegisteredType_SetsActive', async () => {
    const store = createStore();
    const action = { type: CREATE_TYPE, payload: { name: 'Lathe' } };

    await store.dispatch(action);

    expect(store.active()).toEqual(action);
  });

  it('dispatch_WithInvalidPayload_DoesNotOpen', async () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 42 } });

    expect(store.active()).toBeNull();
    expect(logged).toHaveBeenCalledTimes(1);
  });

  it('dispatch_WithUnknownType_DoesNotOpen', async () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();

    await store.dispatch({ type: 'things.unknown', payload: {} });

    expect(store.active()).toBeNull();
    expect(logged).toHaveBeenCalledTimes(1);
  });

  it('complete_Twice_IncreasesSequence', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.complete(SAVED_TYPE);
    const first = store.lastResult();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } });
    store.complete(SAVED_TYPE);
    const second = store.lastResult();

    expect(first).toEqual({ type: SAVED_TYPE, sequence: 1 });
    expect(second).toEqual({ type: SAVED_TYPE, sequence: 2 });
    expect(store.active()).toBeNull();
  });

  it('cancel_Always_ClearsActiveWithoutResult', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.cancel();

    expect(store.active()).toBeNull();
    expect(store.lastResult()).toBeNull();
  });
});
