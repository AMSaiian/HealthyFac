import { effect, inject } from '@angular/core';
import {
  type EmptyFeatureResult,
  getState,
  patchState,
  type SignalStoreFeature,
  signalStoreFeature,
  type,
  withHooks,
} from '@ngrx/signals';

import { BROWSER_STORAGE } from './browser-storage';
import type { StorageKey } from './storage-keys';

export interface StorageSerializer<State extends object> {
  read: (raw: string | null) => Partial<State>;
  write: (state: State) => string;
}

function readRaw(storage: Storage | null, key: StorageKey): string | null {
  try {
    return storage?.getItem(key) ?? null;
  } catch {
    return null;
  }
}

export function withStorageSync<State extends object>(
  key: StorageKey,
  serializer: StorageSerializer<State>,
): SignalStoreFeature<EmptyFeatureResult & { state: State }, EmptyFeatureResult> {
  return signalStoreFeature(
    type<{ state: State }>(),
    withHooks({
      onInit(store) {
        const storage = inject(BROWSER_STORAGE);

        patchState(store, serializer.read(readRaw(storage, key)));

        effect(() => {
          const value = serializer.write(getState(store) as State);
          try {
            storage?.setItem(key, value);
          } catch {
            return;
          }
        });
      },
    }),
  );
}
