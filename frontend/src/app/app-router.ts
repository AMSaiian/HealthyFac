import { withAutoCleanupInjectors, withComponentInputBinding, withRouterConfig } from '@angular/router';

/**
 * The router features of the app and of every route flow test. `computed` keeps the history entries when a guard
 * cancels a browser back or forward navigation.
 */
export const APP_ROUTER_FEATURES = [
  withComponentInputBinding(),
  withAutoCleanupInjectors(),
  withRouterConfig({ canceledNavigationResolution: 'computed' }),
] as const;
