import '@angular/compiler';
import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app.config';
import { bootstrapKcApplication } from './kc.gen';

const LICENSE_KEY_PROPERTY = 'PRIMEUI_LICENSE_KEY';

(async () => {
  const appConfig = createAppConfig(window.kcContext?.properties?.[LICENSE_KEY_PROPERTY] || undefined);

  if (window.kcContext === undefined) {
    const { NoContextComponent } = await import('./no-context');

    await bootstrapApplication(NoContextComponent, appConfig);

    return;
  }

  await bootstrapKcApplication({
    kcContext: window.kcContext,
    bootstrapApplication: ({ KcRootComponent, kcProvider }) =>
      bootstrapApplication(KcRootComponent, {
        ...appConfig,
        providers: [...appConfig.providers, kcProvider],
      }),
  });
})();
