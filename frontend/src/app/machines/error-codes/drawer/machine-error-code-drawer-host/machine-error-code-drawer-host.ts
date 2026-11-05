import { Component, computed, effect, inject, input, output, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { hasFormChanges } from '@shared/forms/form-changes';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { MachineErrorCodeDrawerStore } from '@machines/error-codes/drawer/machine-error-code-drawer.store';
import {
  machineErrorCodeFormSchema,
  type MachineErrorCodeFormValue,
} from '@machines/error-codes/drawer/machine-error-code-form-schema';
import { MachineErrorCodeForm } from '@machines/error-codes/drawer/machine-error-code-form/machine-error-code-form';
import { MachineErrorCodesApi } from '@machines/error-codes/machine-error-codes-api';

@Component({
  imports: [ActionDialog, LoadingSpinner, MachineErrorCodeForm],
  host: { class: 'contents' },
  providers: [MachineErrorCodesApi, MachineErrorCodeDrawerStore],
  selector: 'hf-machine-error-code-drawer-host',
  templateUrl: './machine-error-code-drawer-host.html',
})
export class MachineErrorCodeDrawerHost {
  readonly machineId = input.required<string>();
  readonly errorCodeId = input.required<string | null>();

  readonly saved = output<void>();
  readonly cancelled = output<void>();

  protected readonly store = inject(MachineErrorCodeDrawerStore);

  private readonly _current = signal<MachineErrorCodeFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly hasNoChanges = computed(() => {
    const initial = this.store.initialValue();
    const current = this._current();
    return (
      this.store.isUpdate() &&
      initial !== null &&
      current !== null &&
      !hasFormChanges(machineErrorCodeFormSchema, initial, current)
    );
  });
  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'MACHINE_ERROR_CODES.FORM.INVALID';
    }
    return this.hasNoChanges() ? 'MACHINE_ERROR_CODES.FORM.NO_CHANGES' : null;
  });

  constructor() {
    effect(() => {
      this.store.start(this.machineId(), this.errorCodeId());
    });
  }

  protected onValueChange(value: MachineErrorCodeFormValue): void {
    this._current.set(value);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveDisabledReason() === null) {
      this.store.save({ value, onSaved: () => this.saved.emit() });
    }
  }
}
