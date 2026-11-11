import { DOCUMENT } from '@angular/common';
import { effect, inject, untracked } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';
import { catchError, EMPTY, mergeMap, pipe } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';
import { StorageKey } from '@shared/storage/storage-keys';
import { withStorageSync } from '@shared/storage/with-storage-sync';

import { DEFAULT_LANGUAGE, Language } from './i18n.constants';
import { languageSerializer } from './language-serializer';
import { LocaleApi } from './locale-api';

const PRIMENG_KEY = 'PRIMENG';

const isLanguage = (value: string): value is Language => Object.values<string>(Language).includes(value);

export const LanguageStore = signalStore(
  { providedIn: 'root' },
  withState<{ language: Language }>({ language: DEFAULT_LANGUAGE }),
  withStorageSync(StorageKey.Language, languageSerializer),
  withProps(() => ({ _auth: inject(AuthStore), _localeApi: inject(LocaleApi) })),
  withMethods((store) => {
    const saveLocale = rxMethod<Language>(
      pipe(mergeMap((language) => store._localeApi.save(language).pipe(catchError(() => EMPTY)))),
    );

    return {
      use(language: Language): void {
        patchState(store, { language });
        if (store._auth.isAuthenticated()) {
          saveLocale(language);
        }
      },
    };
  }),
  withHooks({
    onInit(store) {
      const document = inject(DOCUMENT);
      const translate = inject(TranslateService);
      const primeNg = inject(PrimeNG);
      effect(() => {
        const language = store.language();
        document.documentElement.lang = language;
        translate.use(language).subscribe(() => {
          primeNg.setTranslation(translate.instant(PRIMENG_KEY));
        });
      });
      effect(() => {
        store._auth.setUiLocale(store.language());
      });
      effect(() => {
        const locale = store._auth.signInLocale();
        if (locale !== null && isLanguage(locale) && locale !== untracked(store.language)) {
          patchState(store, { language: locale });
        }
      });
    },
  }),
);
