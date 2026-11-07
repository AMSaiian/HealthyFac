import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeUk from '@angular/common/locales/uk';
import type { ApplicationConfig } from '@angular/core';
import { ErrorHandler, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy, withComponentInputBinding } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { routes } from '@app/app.routes';
import { APP_CONFIG, type AppConfig } from '@shared/config/app-config';
import { MachineActionType } from '@machines/machine-actions';
import { provideActionHosts } from '@shared/actions/action-registry';
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

/**
 * Builds the providers from the runtime config, because PrimeNG reads its license key only once, at start.
 */
export function createAppConfig(config: AppConfig): ApplicationConfig {
  return {
    providers: [
      { provide: APP_CONFIG, useValue: config },
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes, withComponentInputBinding()),
      provideAppInitializer(() => inject(AuthStore).init()),
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
      provideActionHosts(
        [MachineActionType.Create, MachineActionType.Update].map((type) => ({
          type,
          load: async () => {
            const [host, form] = await Promise.all([
              import('@machines/drawer/machine-drawer-host/machine-drawer-host'),
              import('@machines/drawer/machine-form/machine-form-schema'),
            ]);
            return { component: host.MachineDrawerHost, schema: form.machineActionPayloadSchema };
          },
        })),
      ),
      MessageService,
      ConfirmationService,
      { provide: ErrorHandler, useClass: AppErrorHandler },
      { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
    ],
  };
}
