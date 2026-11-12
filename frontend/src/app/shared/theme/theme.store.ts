import { DOCUMENT } from '@angular/common';
import { effect, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';

import { StorageKey } from '@shared/storage/storage-keys';
import { withStorageSync } from '@shared/storage/with-storage-sync';

import { themeSerializer } from './theme-serializer';
import { DARK_MODE_CLASS } from './theme.constants';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export const ThemeStore = signalStore(
  { providedIn: 'root' },
  withState({ isDark: false }),
  withStorageSync(StorageKey.Theme, themeSerializer),
  withMethods((store) => ({
    toggle(): void {
      patchState(store, { isDark: !store.isDark() });
    },
  })),
  withHooks({
    onInit(store) {
      const document = inject(DOCUMENT);
      effect(() => {
        document.documentElement.classList.toggle(DARK_MODE_CLASS, store.isDark());
      });
    },
  }),
  withStoreDevtools('theme'),
);
