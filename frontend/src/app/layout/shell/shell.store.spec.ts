import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { Subject } from 'rxjs';

import { EmployeeStore } from '@shared/employee/employee.store';
import { Language } from '@shared/i18n/i18n.constants';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import { languageMenuItems, SHELL_NAV_ITEMS, visibleNavItems } from './shell.store.utils';
import { ShellStore } from './shell.store';

const HOME_PATH = '/';
const MACHINES_PATH = '/machines';

interface Setup {
  store: InstanceType<typeof ShellStore>;
  events: Subject<NavigationStart>;
  confirm: ReturnType<typeof vi.fn>;
  logout: ReturnType<typeof vi.fn>;
}

function setup(options: { isAuthenticated: boolean; hasPermission: boolean; employeeId?: string | null }): Setup {
  const events = new Subject<NavigationStart>();
  const confirm = vi.fn();
  const logout = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      ShellStore,
      { provide: Router, useValue: { events } },
      { provide: EmployeeStore, useValue: { id: signal(options.employeeId ?? null) } },
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
  it('isMenuShown_WithoutASession_IsFalse', () => {
    const { store } = setup({ isAuthenticated: false, hasPermission: true });

    expect(store.isMenuShown()).toBe(false);
  });

  it('isMenuShown_WithASession_IsTrue', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: true });

    expect(store.isMenuShown()).toBe(true);
  });

  it('navItems_WithoutMachinesRead_HidesMachines', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: false });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([HOME_PATH]);
  });

  it('navItems_WithEveryPermissionAndEmployee_ShowsEveryItemWithTheOwnSchedulePath', () => {
    const { store } = setup({ isAuthenticated: true, hasPermission: true, employeeId: 'e 1' });

    const paths = store.navItems().map((item) => item.path);

    expect(paths).toEqual([
      HOME_PATH,
      MACHINES_PATH,
      '/work-orders',
      '/employees',
      '/company/schedule',
      '/employees/e%201/schedule',
      FILES_PATH,
    ]);
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

describe('visibleNavItems', () => {
  const only =
    (...allowed: Permission[]) =>
    (permission: Permission): boolean =>
      allowed.includes(permission);

  it('visibleNavItems_WithoutEmployee_HidesTheOwnSchedule', () => {
    const items = visibleNavItems(SHELL_NAV_ITEMS, only(Permission.ShiftsRead), null);

    expect(items.map((item) => item.path)).toEqual([HOME_PATH]);
  });

  it('visibleNavItems_WorkerWithShiftsRead_ShowsHomeAndOwnSchedule', () => {
    const items = visibleNavItems(SHELL_NAV_ITEMS, only(Permission.ShiftsRead, Permission.WorkOrdersRead), 'e1');

    expect(items.map((item) => item.path)).toEqual([HOME_PATH, '/work-orders', '/employees/e1/schedule']);
  });

  it('visibleNavItems_SupervisorWithShiftsManage_ShowsCompanyScheduleAndEmployees', () => {
    const items = visibleNavItems(
      SHELL_NAV_ITEMS,
      only(Permission.ShiftsRead, Permission.ShiftsManage, Permission.EmployeesRead),
      'e1',
    );

    expect(items.map((item) => item.path)).toEqual([
      HOME_PATH,
      '/employees',
      '/company/schedule',
      '/employees/e1/schedule',
    ]);
  });
});

describe('languageMenuItems', () => {
  it('languageMenuItems_WithTwoLanguages_MarksTheCurrentOneAndKeepsTheNameKeys', () => {
    const items = languageMenuItems(
      [Language.English, Language.Ukrainian],
      Language.Ukrainian,
      (language) => `/${language}.svg`,
      () => undefined,
    );

    expect(items.map((item) => item.label)).toEqual(['LANGUAGE.EN', 'LANGUAGE.UK']);
    expect(items.map((item) => (item.state as { isCurrent: boolean }).isCurrent)).toEqual([false, true]);
  });

  it('languageMenuItems_OnCommand_ChoosesThatLanguage', () => {
    const choose = vi.fn();
    const items = languageMenuItems([Language.English, Language.Ukrainian], Language.English, () => '', choose);

    items[1].command?.({ originalEvent: new Event('click'), item: items[1] });

    expect(choose).toHaveBeenCalledWith(Language.Ukrainian);
  });
});
