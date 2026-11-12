import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export const HomeStore = signalStore(
  withState({ isIntroVisible: true }),
  withMethods((store) => ({
    dismissIntro(): void {
      patchState(store, { isIntroVisible: false });
    },
  })),
  withStoreDevtools('home'),
);
