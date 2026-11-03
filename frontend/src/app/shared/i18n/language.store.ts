import { DOCUMENT } from '@angular/common';
import { effect, inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';

import { StorageKey } from '@shared/storage/storage-keys';
import { withStorageSync } from '@shared/storage/with-storage-sync';

import type { Language } from './i18n.constants';
import { DEFAULT_LANGUAGE } from './i18n.constants';
import { languageSerializer } from './language-serializer';

const PRIMENG_KEY = 'PRIMENG';

export const LanguageStore = signalStore(
  { providedIn: 'root' },
  withState<{ language: Language }>({ language: DEFAULT_LANGUAGE }),
  withStorageSync(StorageKey.Language, languageSerializer),
  withMethods((store) => ({
    use(language: Language): void {
      patchState(store, { language });
    },
  })),
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
    },
  }),
);
