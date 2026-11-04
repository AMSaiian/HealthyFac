import { inject, InjectionToken } from '@angular/core';
import Keycloak from 'keycloak-js';

import { APP_CONFIG } from '@shared/config/app-config';

export const KEYCLOAK = new InjectionToken<Keycloak>('KEYCLOAK', {
  providedIn: 'root',
  factory: () => {
    const config = inject(APP_CONFIG);
    return new Keycloak({ url: config.authUrl, realm: config.authRealm, clientId: config.authClientId });
  },
});
