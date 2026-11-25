import { Component, computed, inject, input, type OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tooltip } from 'primeng/tooltip';

import { tabLink } from '@shared/tabs/tab-route';

import type { EmployeesPage as EmployeesResult, InvitesPage } from '@employees/employee';
import { EMPLOYEES_RETURN_TARGET } from '@employees/employees-return-target';
import { EmployeesGridStore } from '@employees/list/employees-grid.store';
import { EmployeeGrid } from '@employees/list/employee-grid/employee-grid';
import { InviteGrid } from '@employees/invites/invite-grid/invite-grid';
import { InvitesGridStore } from '@employees/invites/invites-grid.store';

import { EMPLOYEES_TAB, EMPLOYEES_TABS, INVITES_TAB } from './employees-page.utils';

const EMPTY_EMPLOYEES: EmployeesResult = { items: [], totalCount: 0 };
const EMPTY_INVITES: InvitesPage = { items: [], totalCount: 0 };

@Component({
  imports: [
    ButtonDirective,
    EmployeeGrid,
    InviteGrid,
    RouterLink,
    Tab,
    TabList,
    TabPanel,
    TabPanels,
    Tabs,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-employees-page',
  templateUrl: './employees-page.html',
})
export class EmployeesPage implements OnInit {
  protected readonly store = inject(EmployeesGridStore);
  protected readonly invitesStore = inject(InvitesGridStore);
  protected readonly EMPLOYEES_TAB = EMPLOYEES_TAB;
  protected readonly INVITES_TAB = INVITES_TAB;
  /** The `tab` route param: the last route segment, which the first tab has not. */
  readonly tab = input<string>();
  /** The invites tab only for a user who may invite; the page does not show it to anyone else. */
  protected readonly activeTab = computed(() =>
    this.tab() === INVITES_TAB && this.store.canInvite() ? INVITES_TAB : EMPLOYEES_TAB,
  );
  protected readonly tabLinks = Object.fromEntries(
    EMPLOYEES_TABS.map((tab) => [tab, tabLink([EMPLOYEES_RETURN_TARGET.url], EMPLOYEES_TABS, tab)]),
  );

  protected readonly page = computed(() =>
    this.store.employees.hasValue() ? this.store.employees.value() : EMPTY_EMPLOYEES,
  );
  protected readonly invites = computed(() =>
    this.invitesStore.invites.hasValue() ? this.invitesStore.invites.value() : EMPTY_INVITES,
  );

  /** A page that opens on the invites tab (a reload, a back navigation) starts the invites grid. */
  ngOnInit(): void {
    if (this.activeTab() === INVITES_TAB) {
      this.invitesStore.activate();
    }
  }

  /** Reloads the grid of the tab that the user opens. The first render does not call it. */
  protected onTabChange(tab: string | number | undefined): void {
    if (tab === INVITES_TAB) {
      this.invitesStore.activate();
    }
    if (tab === EMPLOYEES_TAB) {
      this.store.refresh();
    }
  }
}
