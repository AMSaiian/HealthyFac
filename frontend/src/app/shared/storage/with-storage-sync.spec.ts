import { TestBed } from '@angular/core/testing';
import { patchState, signalStore, withState } from '@ngrx/signals';

import { BROWSER_STORAGE } from './browser-storage';
import { StorageKey } from './storage-keys';
import { FakeStorage } from './testing/fake-storage';
import { withStorageSync } from './with-storage-sync';

interface CounterState {
  count: number;
}

const counterSerializer = {
  read: (raw: string | null): Partial<CounterState> => (raw === null ? {} : { count: Number(raw) }),
  write: (state: CounterState): string => String(state.count),
};

const CounterStore = signalStore(
  { protectedState: false },
  withState<CounterState>({ count: 1 }),
  withStorageSync(StorageKey.Theme, counterSerializer),
);

function createStore(storage: Storage | null): InstanceType<typeof CounterStore> {
  TestBed.configureTestingModule({
    providers: [{ provide: BROWSER_STORAGE, useValue: storage }, CounterStore],
  });
  return TestBed.inject(CounterStore);
}

describe('withStorageSync', () => {
  it('onInit_WhenStorageIsNull_KeepsInitialState', () => {
    const store = createStore(null);

    TestBed.tick();

    expect(store.count()).toBe(1);
  });

  it('onInit_WhenGetItemThrows_KeepsInitialState', () => {
    const storage = new FakeStorage();
    storage.failOnGet = true;
    const store = createStore(storage);

    TestBed.tick();

    expect(store.count()).toBe(1);
  });

  it('stateChange_WhenSetItemThrows_KeepsNewState', () => {
    const storage = new FakeStorage();
    storage.failOnSet = true;
    const store = createStore(storage);

    patchState(store, { count: 5 });
    TestBed.tick();

    expect(store.count()).toBe(5);
  });

  it('onInit_WhenStorageHoldsValue_ReadsItAndWritesChanges', () => {
    const storage = new FakeStorage();
    storage.items.set(StorageKey.Theme, '7');
    const store = createStore(storage);

    TestBed.tick();
    patchState(store, { count: 9 });
    TestBed.tick();

    expect(store.count()).toBe(9);
    expect(storage.items.get(StorageKey.Theme)).toBe('9');
  });
});
