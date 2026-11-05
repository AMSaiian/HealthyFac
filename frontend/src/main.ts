import { isDevMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';

import { App } from '@app/app';
import { createAppConfig } from '@app/app.config';
import { APP_CONFIG_PATH, AppConfigError, parseAppConfig } from '@shared/config/app-config';
import { startErrorMessage } from '@shared/errors/start-error-message';

async function start(): Promise<void> {
  const response = await fetch(APP_CONFIG_PATH);
  if (!response.ok) {
    throw new AppConfigError(`${APP_CONFIG_PATH} returned status ${response.status}.`);
  }
  const config = parseAppConfig(await response.json());
  await bootstrapApplication(App, createAppConfig(config));
}

start().catch((error: unknown) => {
  console.error(error);
  document.body.textContent = startErrorMessage(error, isDevMode());
});
