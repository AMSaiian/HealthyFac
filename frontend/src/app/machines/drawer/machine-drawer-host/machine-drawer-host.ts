import { Component, computed, effect, inject, input, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { hasFormChanges } from '@shared/forms/form-changes';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachinesApi } from '@machines/machines-api';

import { MachineDrawerStore } from '@machines/drawer/machine-drawer.store';
import { machineFormSchema, type MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';
import { MachineForm } from '@machines/drawer/machine-form/machine-form';

@Component({
  imports: [ActionDialog, LoadingSpinner, MachineForm],
  host: { class: 'contents' },
  providers: [MachinesApi, DraftsApi, MachineDrawerStore],
  selector: 'hf-machine-drawer-host',
  templateUrl: './machine-drawer-host.html',
})
export class MachineDrawerHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(MachineDrawerStore);
  protected readonly showSpinner = delayedLoading(computed(() => !this.store.prefilledValue()));
  protected readonly showSaving = delayedLoading(this.store.isSaving);

  private readonly _current = signal<MachineFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly hasNoChanges = computed(() => {
    const initial = this.store.initialValue();
    const current = this._current();
    return (
      this.store.isUpdate() &&
      initial !== null &&
      current !== null &&
      !hasFormChanges(machineFormSchema, initial, current)
    );
  });
  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'FORMS.INVALID';
    }
    return this.hasNoChanges() ? 'FORMS.NO_CHANGES' : null;
  });

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }

  protected onValueChange(value: MachineFormValue): void {
    this._current.set(value);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveDisabledReason() === null) {
      this.store.save(value);
    }
  }
}
