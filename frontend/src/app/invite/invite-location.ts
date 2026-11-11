import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

const TOKEN_KEY = 'token';

/**
 * Reads the invite token from the fragment of the address and removes the fragment, so the token stays out of the
 * history and of any later request. The store test replaces it.
 */
@Injectable()
export class InviteLocation {
  private readonly _document = inject(DOCUMENT);

  /** Gives the token of the fragment (`#token=...`), or `null` when there is none. Never logs it. */
  readToken(): string | null {
    const fragment = this._document.location.hash.replace(/^#/, '');
    const token = new URLSearchParams(fragment).get(TOKEN_KEY);
    return token === null || token === '' ? null : token;
  }

  clearFragment(): void {
    const { pathname, search } = this._document.location;
    this._document.defaultView?.history.replaceState(null, '', `${pathname}${search}`);
  }
}
