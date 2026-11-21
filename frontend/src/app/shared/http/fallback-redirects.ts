import { Injectable } from '@angular/core';

/**
 * Remembers that this document has already sent a navigation to a fallback page. A fallback page that a reload
 * restored has not: it asks for the requested page again (`retryRequestedUrlAfterReload`).
 */
@Injectable({ providedIn: 'root' })
export class FallbackRedirects {
  isRedirected = false;
}
