import { computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationStart, Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { filter } from 'rxjs';

import { withConfirm } from '@shared/confirm/with-confirm';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { SHELL_NAV_ITEMS, type ShellNavItem, visibleNavItems } from './shell.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export const ShellStore = signalStore(
  withState({ isMenuOpen: false }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _employee: inject(EmployeeStore),
    _router: inject(Router),
  })),
  withConfirm(),
  withComputed((store) => ({
    isMenuShown: computed(() => store._auth.isAuthenticated()),
    navItems: computed((): readonly ShellNavItem[] =>
      store._auth.isAuthenticated()
        ? visibleNavItems(SHELL_NAV_ITEMS, (permission) => store._auth.hasPermission(permission), store._employee.id())
        : [],
    ),
  })),
  withMethods((store) => ({
    toggleMenu(): void {
      patchState(store, { isMenuOpen: !store.isMenuOpen() });
    },
    closeMenu(): void {
      patchState(store, { isMenuOpen: false });
    },
    /** Asks for a confirmation and signs out only after the accept. */
    confirmSignOut(): void {
      store._confirm({
        headerKey: 'SHELL.SIGN_OUT_CONFIRM.HEADER',
        messageKey: 'SHELL.SIGN_OUT_CONFIRM.MESSAGE',
        acceptKey: 'SHELL.SIGN_OUT_CONFIRM.ACCEPT',
        icon: 'pi pi-sign-out',
        onAccept: () => {
          store._auth.logout();
        },
      });
    },
  })),
  withHooks({
    onInit(store) {
      store._router.events
        .pipe(
          filter((event) => event instanceof NavigationStart),
          takeUntilDestroyed(),
        )
        .subscribe(() => store.closeMenu());
    },
  }),
  withStoreDevtools('shell'),
);
