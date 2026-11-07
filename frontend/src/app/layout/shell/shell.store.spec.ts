import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router } from '@angular/router';
import { Subject } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';

import { SHELL_NAV_ITEMS } from './shell.store.utils';
import { ShellStore } from './shell.store';

const HOME_PATH = '/';
const MACHINES_PATH = '/machines';

interface Setup {
  store: InstanceType<typeof ShellStore>;
  events: Subject<NavigationStart>;
}

function setup(options: { isAuthenticated: boolean; hasPermission: boolean }): Setup {
  const events = new Subject<NavigationStart>();
  TestBed.configureTestingModule({
    providers: [
      ShellStore,
      { provide: Router, useValue: { events } },
      {
        provide: AuthStore,
        useValue: {
          isAuthenticated: signal(options.isAuthenticated),
          hasPermission: () => options.hasPermission,
        },
      },
    ],
  });
  return { store: TestBed.inject(ShellStore), events };
}

describe('ShellStore', () => {
  it('navItems_WithoutMachinesRead_HidesMachines', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: false });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([HOME_PATH]);
  });

  it('navItems_WithMachinesRead_ShowsEveryItem', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: true });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([HOME_PATH, MACHINES_PATH]);
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
});
