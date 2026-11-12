import { patchState, signalStoreFeature, withMethods, withState } from '@ngrx/signals';

interface LocaleState {
  uiLocale: string | undefined;
}

/** The UI locale of the user. The sign-in page takes it as the language of the login screen. */
export function withAuthLocale() {
  return signalStoreFeature(
    withState<LocaleState>({ uiLocale: undefined }),
    withMethods((store) => ({
      setUiLocale(uiLocale: string): void {
        patchState(store, { uiLocale });
      },
    })),
  );
}
