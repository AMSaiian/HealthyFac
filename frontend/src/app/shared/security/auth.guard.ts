import { inject } from '@angular/core';
import type { CanActivateFn } from '@angular/router';

import { AuthStore } from './auth.store';

export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(AuthStore);
  if (store.isAuthenticated()) {
    return true;
  }
  void store.login(state.url);
  return false;
};
