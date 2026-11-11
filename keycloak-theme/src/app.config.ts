import type { ApplicationConfig } from '@angular/core';
import { providePrimeNG } from 'primeng/config';
import { HealthyFacPreset } from '@frontend-preset';

export const DARK_MODE_CLASS = 'app-dark';

/** Builds the providers. The PrimeUI license key is public by design: PrimeNG checks it in the browser. */
export function createAppConfig(licenseKey: string | undefined): ApplicationConfig {
  return {
    providers: [
      providePrimeNG({
        license: licenseKey,
        theme: {
          preset: HealthyFacPreset,
          options: {
            darkModeSelector: `.${DARK_MODE_CLASS}`,
            cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
          },
        },
      }),
    ],
  };
}
