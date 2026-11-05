import { Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { EmployeeStore } from '@shared/employee/employee.store';

import type { MachinesPage as MachinesResult } from '@machines/machine';

import { MachineGrid } from '@machines/list/machine-grid/machine-grid';
import { MachinesGridStore } from '@machines/list/machines-grid.store';

const EMPTY_PAGE: MachinesResult = { items: [], totalCount: 0 };

@Component({
  imports: [ButtonDirective, MachineGrid, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-machines-page',
  templateUrl: './machines-page.html',
})
export class MachinesPage {
  protected readonly store = inject(MachinesGridStore);
  protected readonly employee = inject(EmployeeStore);

  protected readonly page = computed(() => (this.store.machines.hasValue() ? this.store.machines.value() : EMPTY_PAGE));
}
