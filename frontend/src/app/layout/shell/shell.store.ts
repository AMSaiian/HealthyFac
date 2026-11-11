import { computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationStart, Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { filter } from 'rxjs';

import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth.store';

import { SHELL_NAV_ITEMS, type ShellNavItem, visibleNavItems } from './shell.store.utils';

export const ShellStore = signalStore(
  withState({ isMenuOpen: false }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _confirmation: inject(ConfirmationService),
    _employee: inject(EmployeeStore),
    _router: inject(Router),
    _translate: inject(TranslateService),
  })),
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
      store._confirmation.confirm({
        header: store._translate.instant('SHELL.SIGN_OUT_CONFIRM.HEADER'),
        message: store._translate.instant('SHELL.SIGN_OUT_CONFIRM.MESSAGE'),
        icon: 'pi pi-sign-out',
        acceptLabel: store._translate.instant('SHELL.SIGN_OUT_CONFIRM.ACCEPT'),
        rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
        rejectButtonProps: { severity: 'secondary' },
        accept: () => {
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
);
