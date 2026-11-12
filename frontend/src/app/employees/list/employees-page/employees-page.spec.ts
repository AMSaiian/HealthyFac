import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import type { ActionResult } from '@shared/actions/app-action';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { InvitesGridStore } from '@employees/invites/invites-grid.store';
import { EmployeesApi } from '@employees/employees-api';
import { EmployeesGridStore } from '@employees/list/employees-grid.store';

import { EmployeesPage } from './employees-page';

const EMPLOYEES_TAB = 'employees';
const INVITES_TAB = 'invites';

function changeTab(page: EmployeesPage, tab: string): void {
  Reflect.apply(Reflect.get(page, 'onTabChange'), page, [tab]);
}

describe('EmployeesPage', () => {
  const lastResult = signal<ActionResult | null>(null);
  const searchEmployees = vi.fn();
  const searchInvites = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<EmployeesPage> {
    TestBed.configureTestingModule({
      providers: [
        EmployeesGridStore,
        InvitesGridStore,
        { provide: EmployeesApi, useValue: { searchEmployees, searchInvites } },
        { provide: ActionStore, useValue: { dispatch: vi.fn(), lastResult } },
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
      ],
    });
    const page = TestBed.runInInjectionContext(() => new EmployeesPage());
    await settle();
    return page;
  }

  beforeEach(() => {
    lastResult.set(null);
    searchEmployees.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
    searchInvites.mockReset().mockReturnValue(of({ items: [], totalCount: 0 }));
  });

  it('onTabChange_ToEachTab_RefreshesItsStore', async () => {
    const page = await setup();
    const employeesAtStart = searchEmployees.mock.calls.length;

    changeTab(page, INVITES_TAB);
    await settle();
    const invitesAfterFirstOpen = searchInvites.mock.calls.length;

    changeTab(page, EMPLOYEES_TAB);
    await settle();
    const employeesAfterReturn = searchEmployees.mock.calls.length;

    changeTab(page, INVITES_TAB);
    await settle();

    expect(invitesAfterFirstOpen).toBe(1);
    expect(employeesAfterReturn - employeesAtStart).toBe(1);
    expect(searchInvites).toHaveBeenCalledTimes(2);
  });
});
