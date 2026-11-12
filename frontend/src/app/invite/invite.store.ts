import { HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { httpErrorKey, isHttpStatus } from '@shared/http/http-error-key';
import { withToasts } from '@shared/toasts/with-toasts';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { type AcceptBody, InviteApi, type InviteDetails } from '@invite/invite-api';
import { InviteLocation } from '@invite/invite-location';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export type InviteView = 'loading' | 'form' | 'invalid' | 'employed' | 'success' | 'failed' | 'signedIn';

interface InviteState {
  view: InviteView;
  details: InviteDetails | null;
  isSaving: boolean;
}

const INVITE_PATH = '/invite';
const TOKEN_FRAGMENT_KEY = 'token';

/**
 * The public invite page: it reads the token once from the fragment of the address, removes the fragment, verifies
 * the invite, and accepts it with the name of the new employee. A missing token sends no request.
 */
export const InviteStore = signalStore(
  withState<InviteState>({ view: 'loading', details: null, isSaving: false }),
  withProps(() => ({
    _api: inject(InviteApi),
    _auth: inject(AuthStore),
    _location: inject(InviteLocation),
    _token: { value: null as string | null },
  })),
  withToasts(),
  withMethods((store) => {
    const fail = (error: unknown): void => {
      if (isHttpStatus(error, HttpStatusCode.NotFound)) {
        patchState(store, { view: 'invalid' });
      } else if (isHttpStatus(error, HttpStatusCode.Conflict)) {
        patchState(store, { view: 'employed' });
      } else {
        patchState(store, { view: 'failed' });
        store._toastError(httpErrorKey(error));
      }
    };

    const verify = rxMethod<string>(
      pipe(
        tap(() => patchState(store, { view: 'loading' })),
        exhaustMap((token) =>
          store._api.verify(token).pipe(
            tap((details) => patchState(store, { details, view: 'form' })),
            catchError((error: unknown) => {
              fail(error);
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    const accept = rxMethod<AcceptBody>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap((body) => {
          const token = store._token.value;
          if (token === null) {
            patchState(store, { view: 'invalid', isSaving: false });
            return EMPTY;
          }
          return store._api.accept(token, body).pipe(
            tap(() => patchState(store, { view: 'success' })),
            catchError((error: unknown) => {
              fail(error);
              return EMPTY;
            }),
            finalize(() => patchState(store, { isSaving: false })),
          );
        }),
      ),
    );

    return {
      /** Reads the token, clears the fragment, and verifies the invite. */
      start(): void {
        const token = store._location.readToken();
        store._location.clearFragment();
        store._token.value = token;
        if (token === null) {
          patchState(store, { view: 'invalid' });
          return;
        }
        if (store._auth.isAuthenticated()) {
          patchState(store, { view: 'signedIn' });
          return;
        }
        verify(token);
      },
      /** Ends the session of the signed-in user and returns to this page with the token in the fragment. */
      async signOutAndContinue(): Promise<void> {
        const token = store._token.value;
        await store._auth.logout(
          token === null ? INVITE_PATH : `${INVITE_PATH}#${TOKEN_FRAGMENT_KEY}=${encodeURIComponent(token)}`,
        );
      },
      accept,
    };
  }),
  withHooks({
    onInit(store) {
      store.start();
    },
  }),
  withStoreDevtools('invite'),
);
