import { Location } from '@angular/common';
import { inject } from '@angular/core';
import { Router } from '@angular/router';

import { FallbackRedirects } from '@shared/http/fallback-redirects';

/**
 * Asks for the requested page again when a reload restored a fallback page. With `browserUrl`, the history entry of
 * a fallback page stores the fallback route, and a reload navigates to that route, not to the address bar URL. When
 * the navigation that activates the page is the first one of the app, the app has not sent a navigation to a fallback
 * page itself, and the address bar shows another URL, this navigates to the address bar URL and replaces the entry. Call it in the constructor of a fallback page.
 *
 * @param fallbackUrl The route of the fallback page.
 */
export function retryRequestedUrlAfterReload(fallbackUrl: string): void {
  const router = inject(Router);
  const requestedUrl = inject(Location).path();
  const isFirstNavigation = router.currentNavigation()?.previousNavigation === null;
  if (!isFirstNavigation || inject(FallbackRedirects).isRedirected || requestedUrl === fallbackUrl) {
    return;
  }
  void router.navigateByUrl(requestedUrl, { replaceUrl: true });
}
