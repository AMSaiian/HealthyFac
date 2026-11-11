import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { Language } from './i18n.constants';

const LOCALE_URL = '/api/auth/v1/me/locale';

/** Saves the language of the signed-in user in Keycloak, so the login pages open in it. */
@Injectable({ providedIn: 'root' })
export class LocaleApi {
  private readonly _http = inject(HttpClient);

  save(language: Language): Observable<void> {
    return this._http.put<void>(
      LOCALE_URL,
      { locale: language },
      { context: new HttpContext().set(SKIP_ERROR_TOAST, true) },
    );
  }
}
