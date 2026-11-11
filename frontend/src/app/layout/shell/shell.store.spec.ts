import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { Subject } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';

import { SHELL_NAV_ITEMS } from './shell.store.utils';
import { ShellStore } from './shell.store';

const HOME_PATH = '/';
const MACHINES_PATH = '/machines';

interface Setup {
  store: InstanceType<typeof ShellStore>;
  events: Subject<NavigationStart>;
  confirm: ReturnType<typeof vi.fn>;
  logout: ReturnType<typeof vi.fn>;
}

function setup(options: { isAuthenticated: boolean; hasPermission: boolean }): Setup {
  const events = new Subject<NavigationStart>();
  const confirm = vi.fn();
  const logout = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      ShellStore,
      { provide: Router, useValue: { events } },
      {
        provide: AuthStore,
        useValue: {
          isAuthenticated: signal(options.isAuthenticated),
          hasPermission: () => options.hasPermission,
          logout,
        },
      },
      { provide: ConfirmationService, useValue: { confirm } },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  return { store: TestBed.inject(ShellStore), events, confirm, logout };
}

const FILES_PATH = '/files';
const SIGN_OUT_HEADER_KEY = 'SHELL.SIGN_OUT_CONFIRM.HEADER';

describe('ShellStore', () => {
  it('navItems_WithoutMachinesRead_HidesMachines', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: false });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([HOME_PATH]);
  });

  it('navItems_WithMachinesRead_ShowsEveryItem', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: true });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([HOME_PATH, MACHINES_PATH, FILES_PATH]);
    expect(store.navItems()).toEqual(SHELL_NAV_ITEMS);
  });

  it('navItems_WhenSignedOut_IsEmpty', () => {
    const { store } = setup({ isAuthenticated: false, hasPermission: true });

    expect(store.navItems()).toEqual([]);
  });

  it('navigationStart_WhenMenuOpen_ClosesMenu', () => {
    const { store, events } = setup({ isAuthenticated: true, hasPermission: true });
    store.toggleMenu();

    events.next(new NavigationStart(1, MACHINES_PATH));

    expect(store.isMenuOpen()).toBe(false);
  });

  it('confirmSignOut_Always_OpensTheConfirmationWithoutSigningOut', () => {
    const { store, confirm, logout } = setup({ isAuthenticated: true, hasPermission: true });

    store.confirmSignOut();

    expect(confirm).toHaveBeenCalledOnce();
    expect(confirm.mock.calls[0][0].header).toBe(SIGN_OUT_HEADER_KEY);
    expect(logout).not.toHaveBeenCalled();
  });

  it('confirmSignOut_WhenAccepted_SignsOutOnce', () => {
    const { store, confirm, logout } = setup({ isAuthenticated: true, hasPermission: true });
    store.confirmSignOut();

    confirm.mock.calls[0][0].accept();

    expect(logout).toHaveBeenCalledOnce();
  });

  it('confirmSignOut_WhenRejected_DoesNotSignOut', () => {
    const { store, confirm, logout } = setup({ isAuthenticated: true, hasPermission: true });
    store.confirmSignOut();

    confirm.mock.calls[0][0].reject?.();

    expect(logout).not.toHaveBeenCalled();
  });
});
