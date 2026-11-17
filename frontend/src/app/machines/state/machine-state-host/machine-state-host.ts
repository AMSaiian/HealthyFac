import { Component, computed, effect, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Select } from 'primeng/select';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { MachineState } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import { STATE_LABEL_KEYS } from '@machines/list/machine-grid/machine-grid.utils';
import { MachineStateStore } from '@machines/state/machine-state.store';

@Component({
  imports: [ActionDialog, FormsModule, LoadingSpinner, Select, TranslatePipe],
  host: { class: 'contents' },
  providers: [MachinesApi, MachineStateStore],
  selector: 'hf-machine-state-host',
  templateUrl: './machine-state-host.html',
})
export class MachineStateHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(MachineStateStore);
  protected readonly showSpinner = delayedLoading(computed(() => this.store.currentState() === null));
  protected readonly showSaving = delayedLoading(this.store.isSaving);
  protected readonly stateOptions = Object.values(MachineState).map((value) => ({
    value,
    labelKey: STATE_LABEL_KEYS[value],
  }));

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
