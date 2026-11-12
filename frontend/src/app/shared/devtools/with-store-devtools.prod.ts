import { withDevToolsStub } from '@angular-architects/ngrx-toolkit';

/**
 * Production build: the production file replacement swaps this file in for `with-store-devtools.ts`, so the Redux
 * DevTools code stays out of the bundle. The stub adds nothing to the store.
 */
export function withStoreDevtools(name: string) {
  return withDevToolsStub(name);
}
