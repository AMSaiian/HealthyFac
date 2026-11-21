import { inject } from '@angular/core';
import {
  type NavigationError,
  RedirectCommand,
  Router,
  withAutoCleanupInjectors,
  withComponentInputBinding,
  withNavigationErrorHandler,
  withRouterConfig,
} from '@angular/router';

import { FallbackRedirects } from '@shared/http/fallback-redirects';
import { NETWORK_ERROR_URL } from '@shared/http/fallback-urls';

/** The message of the `TypeError` that a failed lazy chunk import throws in Chrome, Firefox, and Safari. */
const CHUNK_LOAD_FAILURE_PATTERN = /dynamically imported module|Importing a module script failed/i;

/**
 * Sends the navigation whose lazy chunk failed to load to the network page and keeps the requested address in the
 * address bar (`browserUrl`), so Retry asks for the same page. Any other navigation error keeps the router handling.
 */
export function handleChunkLoadError(error: NavigationError): RedirectCommand | undefined {
  if (!(error.error instanceof TypeError) || !CHUNK_LOAD_FAILURE_PATTERN.test(error.error.message)) {
    return undefined;
  }
  inject(FallbackRedirects).isRedirected = true;
  return new RedirectCommand(inject(Router).parseUrl(NETWORK_ERROR_URL), { browserUrl: error.url });
}

/**
 * The router features of the app and of every route flow test. `computed` keeps the history entries when a guard
 * cancels a browser back or forward navigation.
 */
export const APP_ROUTER_FEATURES = [
  withComponentInputBinding(),
  withAutoCleanupInjectors(),
  withRouterConfig({ canceledNavigationResolution: 'computed' }),
  withNavigationErrorHandler(handleChunkLoadError),
] as const;
