import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';
import type { Permission } from './permissions';

const HOME_URL = '/';

/** Lets the route activate only for a user with the permission. Others go to the home page. */
export function permissionGuard(permission: Permission): CanActivateFn {
  return () => (inject(AuthStore).hasPermission(permission) ? true : inject(Router).parseUrl(HOME_URL));
}
