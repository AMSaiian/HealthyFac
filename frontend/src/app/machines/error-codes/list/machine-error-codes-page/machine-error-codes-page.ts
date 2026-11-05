import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import type { MachineErrorCodesPage as MachineErrorCodesResult } from '@machines/error-codes/machine-error-code';
import { MachineErrorCodeDrawerHost } from '@machines/error-codes/drawer/machine-error-code-drawer-host/machine-error-code-drawer-host';
import { MachineErrorCodeGrid } from '@machines/error-codes/list/machine-error-code-grid/machine-error-code-grid';
import { MachineErrorCodesGridStore } from '@machines/error-codes/list/machine-error-codes-grid.store';

const EMPTY_PAGE: MachineErrorCodesResult = { items: [], totalCount: 0 };

@Component({
  imports: [ButtonDirective, MachineErrorCodeDrawerHost, MachineErrorCodeGrid, RouterLink, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-machine-error-codes-page',
  templateUrl: './machine-error-codes-page.html',
})
export class MachineErrorCodesPage {
  protected readonly store = inject(MachineErrorCodesGridStore);

  protected readonly page = computed(() =>
    this.store.errorCodes.hasValue() ? this.store.errorCodes.value() : EMPTY_PAGE,
  );
}
