import { computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { filter } from 'rxjs';

import { ChatStore } from '@shared/chat/chat.store';
import { withConfirm } from '@shared/confirm/with-confirm';
import { EmployeeStore } from '@shared/employee/employee.store';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import { isAssistantUrl, SHELL_NAV_ITEMS, type ShellNavItem, visibleNavItems } from './shell.store.utils';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

export const ShellStore = signalStore(
  withState({ isMenuOpen: false, currentUrl: '' }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _chat: inject(ChatStore),
    _employee: inject(EmployeeStore),
    _realtime: inject(RealtimeConnection),
    _router: inject(Router),
  })),
  withConfirm(),
  withComputed((store) => ({
    isMenuShown: computed(() => store._auth.isAuthenticated()),
    /** The chat drawer button shows with the chat permission, and never on the assistant page itself. */
    isChatButtonShown: computed(
      () =>
        store._auth.isAuthenticated() &&
        store._auth.hasPermission(Permission.ChatsRead) &&
        !isAssistantUrl(store.currentUrl()),
    ),
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
    /** Records the URL after a navigation. The assistant page closes the chat drawer. */
    setUrl(url: string): void {
      patchState(store, { currentUrl: url });
      if (isAssistantUrl(url)) {
        store._chat.closeDrawer();
      }
    },
    /** Asks for a confirmation and signs out only after the accept. The real-time connection stops first. */
    confirmSignOut(): void {
      store._confirm({
        headerKey: 'SHELL.SIGN_OUT_CONFIRM.HEADER',
        messageKey: 'SHELL.SIGN_OUT_CONFIRM.MESSAGE',
        acceptKey: 'SHELL.SIGN_OUT_CONFIRM.ACCEPT',
        icon: 'pi pi-sign-out',
        onAccept: () => {
          void store._realtime.stop().then(() => store._auth.logout());
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
      store.setUrl(store._router.url);
      store._router.events
        .pipe(
          filter((event) => event instanceof NavigationEnd),
          takeUntilDestroyed(),
        )
        .subscribe((event) => {
          store.setUrl(event.urlAfterRedirects);
        });
    },
  }),
  withStoreDevtools('shell'),
);
