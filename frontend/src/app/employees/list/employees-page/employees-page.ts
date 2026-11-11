import { Component, computed, effect, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';

import type { EmployeesPage as EmployeesResult, InvitesPage } from '@employees/employee';
import { EmployeesGridStore } from '@employees/list/employees-grid.store';
import { EmployeeGrid } from '@employees/list/employee-grid/employee-grid';
import { InviteGrid } from '@employees/invites/invite-grid/invite-grid';
import { InvitesGridStore } from '@employees/invites/invites-grid.store';

const EMPTY_EMPLOYEES: EmployeesResult = { items: [], totalCount: 0 };
const EMPTY_INVITES: InvitesPage = { items: [], totalCount: 0 };
const EMPLOYEES_TAB = 'employees';
const INVITES_TAB = 'invites';

@Component({
  imports: [ButtonDirective, EmployeeGrid, InviteGrid, Tab, TabList, TabPanel, TabPanels, Tabs, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-employees-page',
  templateUrl: './employees-page.html',
})
export class EmployeesPage {
  protected readonly store = inject(EmployeesGridStore);
  protected readonly invitesStore = inject(InvitesGridStore);
  protected readonly EMPLOYEES_TAB = EMPLOYEES_TAB;
  protected readonly INVITES_TAB = INVITES_TAB;
  protected readonly tab = signal(EMPLOYEES_TAB);

  protected readonly page = computed(() =>
    this.store.employees.hasValue() ? this.store.employees.value() : EMPTY_EMPLOYEES,
  );
  protected readonly invites = computed(() =>
    this.invitesStore.invites.hasValue() ? this.invitesStore.invites.value() : EMPTY_INVITES,
  );

  constructor() {
    effect(() => {
      if (this.tab() === INVITES_TAB) {
        this.invitesStore.activate();
      }
    });
  }
}
