import { bootstrapApplication } from '@angular/platform-browser';

import { App } from '@app/app';
import { appConfig } from '@app/app.config';
import { APP_CONFIG, APP_CONFIG_PATH, AppConfigError, parseAppConfig } from '@shared/config/app-config';

async function start(): Promise<void> {
  const response = await fetch(APP_CONFIG_PATH);
  if (!response.ok) {
    throw new AppConfigError(`${APP_CONFIG_PATH} returned status ${response.status}.`);
  }
  const config = parseAppConfig(await response.json());
  await bootstrapApplication(App, {
    ...appConfig,
    providers: [...appConfig.providers, { provide: APP_CONFIG, useValue: config }],
  });
}

start().catch((error: unknown) => {
  console.error(error);
  document.body.textContent =
    error instanceof AppConfigError
      ? `HealthyFac cannot start. The configuration is not valid: ${error.message}`
      : 'HealthyFac cannot start. Reload the page or contact support.';
});
