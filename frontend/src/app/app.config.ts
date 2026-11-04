import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeUk from '@angular/common/locales/uk';
import type { ApplicationConfig } from '@angular/core';
import { ErrorHandler, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { routes } from '@app/app.routes';
import { AppErrorHandler } from '@shared/errors/app-error-handler';
import { apiBaseUrlInterceptor } from '@shared/http/api-base-url.interceptor';
import { problemDetailsInterceptor } from '@shared/http/problem-details.interceptor';
import { DEFAULT_LANGUAGE, TRANSLATION_PATH, TRANSLATION_SUFFIX } from '@shared/i18n/i18n.constants';
import { TranslatedTitleStrategy } from '@shared/i18n/translated-title-strategy';
import { AuthStore } from '@shared/security/auth.store';
import { authInterceptor } from '@shared/security/auth.interceptor';
import { readCspNonce } from '@shared/security/csp-nonce';
import { HealthyFacPreset } from '@shared/theme/healthyfac-preset';
import { DARK_MODE_CLASS } from '@shared/theme/theme.constants';

registerLocaleData(localeUk);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAppInitializer(() => inject(AuthStore).init()),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiBaseUrlInterceptor, problemDetailsInterceptor, authInterceptor]),
    ),
    provideTranslateService({
      fallbackLang: DEFAULT_LANGUAGE,
      loader: provideTranslateHttpLoader({ prefix: TRANSLATION_PATH, suffix: TRANSLATION_SUFFIX }),
    }),
    providePrimeNG({
      csp: { nonce: readCspNonce(document.querySelector('hf-root')) },
      theme: {
        preset: HealthyFacPreset,
        options: {
          darkModeSelector: `.${DARK_MODE_CLASS}`,
          cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
        },
      },
    }),
    MessageService,
    { provide: ErrorHandler, useClass: AppErrorHandler },
    { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
  ],
};
