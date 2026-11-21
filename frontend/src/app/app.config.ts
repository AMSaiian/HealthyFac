import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeUk from '@angular/common/locales/uk';
import type { ApplicationConfig } from '@angular/core';
import { ErrorHandler, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { APP_ACTION_HOSTS } from '@app/app-action-hosts';
import { APP_ROUTER_FEATURES } from '@app/app-router';
import { routes } from '@app/app.routes';
import { APP_CONFIG, type AppConfig } from '@shared/config/app-config';
import { provideActionHosts } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';
import { AppErrorHandler } from '@shared/errors/app-error-handler';
import { apiBaseUrlInterceptor } from '@shared/http/api-base-url.interceptor';
import { problemDetailsInterceptor } from '@shared/http/problem-details.interceptor';
import { DEFAULT_LANGUAGE, TRANSLATION_PATH, TRANSLATION_SUFFIX } from '@shared/i18n/i18n.constants';
import { TranslatedTitleStrategy } from '@shared/i18n/translated-title-strategy';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { authInterceptor } from '@shared/security/auth.interceptor';
import { readCspNonce } from '@shared/security/csp-nonce';
import { HealthyFacPreset } from '@shared/theme/healthyfac-preset';
import { DARK_MODE_CLASS } from '@shared/theme/theme.constants';

registerLocaleData(localeUk);

/**
 * Builds the providers from the runtime config, because PrimeNG reads its license key only once, at start.
 */
export function createAppConfig(config: AppConfig): ApplicationConfig {
  return {
    providers: [
      { provide: APP_CONFIG, useValue: config },
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes, ...APP_ROUTER_FEATURES),
      provideAppInitializer(() => inject(AuthStore).init()),
      provideAppInitializer(() => {
        inject(ActionStore);
      }),
      provideHttpClient(withInterceptors([apiBaseUrlInterceptor, problemDetailsInterceptor, authInterceptor])),
      provideTranslateService({
        fallbackLang: DEFAULT_LANGUAGE,
        loader: provideTranslateHttpLoader({ prefix: TRANSLATION_PATH, suffix: TRANSLATION_SUFFIX }),
      }),
      providePrimeNG({
        license: config.primeUiLicenseKey,
        csp: { nonce: readCspNonce(document.querySelector('hf-root')) },
        theme: {
          preset: HealthyFacPreset,
          options: {
            darkModeSelector: `.${DARK_MODE_CLASS}`,
            cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
          },
        },
      }),
      provideActionHosts(APP_ACTION_HOSTS),
      MessageService,
      ConfirmationService,
      { provide: ErrorHandler, useClass: AppErrorHandler },
      { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
    ],
  };
}
