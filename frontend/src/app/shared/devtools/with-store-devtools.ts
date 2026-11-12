import { isDevMode } from '@angular/core';
import { withDevtools, withDevToolsStub } from '@angular-architects/ngrx-toolkit';

/**
 * Connects a store to the Redux DevTools. Only in development mode; elsewhere it adds nothing. The name is the store
 * file name without `.store.ts`.
 */
export function withStoreDevtools(name: string) {
  return isDevMode() ? withDevtools(name) : withDevToolsStub(name);
}
