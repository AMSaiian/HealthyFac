import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

export const HomeStore = signalStore(
  withState({ isIntroVisible: true }),
  withMethods((store) => ({
    dismissIntro(): void {
      patchState(store, { isIntroVisible: false });
    },
  })),
);
