import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

export const ShellStore = signalStore(
  withState({ isMenuOpen: false }),
  withMethods((store) => ({
    toggleMenu(): void {
      patchState(store, { isMenuOpen: !store.isMenuOpen() });
    },
  })),
);
