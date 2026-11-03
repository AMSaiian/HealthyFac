import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { APP_CONFIG } from '@shared/config/app-config';

const API_PATH_PREFIX = '/api/';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(API_PATH_PREFIX)) {
    return next(request);
  }
  const config = inject(APP_CONFIG);
  return next(request.clone({ url: `${config.apiBaseUrl}${request.url}` }));
};
