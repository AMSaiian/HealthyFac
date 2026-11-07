import { computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationStart, Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { filter } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';

import { SHELL_NAV_ITEMS, type ShellNavItem } from './shell.store.utils';

export const ShellStore = signalStore(
  withState({ isMenuOpen: false }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _router: inject(Router),
  })),
  withComputed((store) => ({
    navItems: computed((): readonly ShellNavItem[] =>
      store._auth.isAuthenticated()
        ? SHELL_NAV_ITEMS.filter((item) => item.permission === undefined || store._auth.hasPermission(item.permission))
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
