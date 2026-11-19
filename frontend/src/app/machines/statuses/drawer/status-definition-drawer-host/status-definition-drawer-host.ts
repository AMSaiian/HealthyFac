import { Component, computed, effect, inject, input, output, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { hasFormChanges } from '@shared/forms/form-changes';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { StatusDefinitionDrawerStore } from '@machines/statuses/drawer/status-definition-drawer.store';
import {
  statusDefinitionFormSchema,
  type StatusDefinitionFormValue,
} from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';
import { StatusDefinitionDetails } from '@machines/statuses/drawer/status-definition-details/status-definition-details';
import { StatusDefinitionForm } from '@machines/statuses/drawer/status-definition-form/status-definition-form';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

@Component({
  imports: [ActionDialog, LoadingSpinner, StatusDefinitionDetails, StatusDefinitionForm],
  host: { class: 'contents' },
  providers: [StatusDefinitionsApi, StatusDefinitionDrawerStore],
  selector: 'hf-status-definition-drawer-host',
  templateUrl: './status-definition-drawer-host.html',
})
export class StatusDefinitionDrawerHost {
  readonly machineId = input.required<string>();
  readonly statusDefinitionId = input.required<string | null>();

  readonly saved = output<void>();
  readonly updated = output<void>();
  readonly cancelled = output<void>();

  protected readonly store = inject(StatusDefinitionDrawerStore);
  protected readonly showSpinner = delayedLoading(computed(() => !this.store.initialValue()));
  protected readonly showSaving = delayedLoading(this.store.isSaving);

  private readonly _current = signal<StatusDefinitionFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly hasNoChanges = computed(() => {
    const initial = this.store.initialValue();
    const current = this._current();
    return (
      this.store.isUpdate() &&
      initial !== null &&
      current !== null &&
      !hasFormChanges(statusDefinitionFormSchema, initial, current)
    );
  });
  protected readonly title = computed((): string => {
    if (this.store.isViewMode()) {
      return 'STATUS_DEFINITIONS.FORM.DETAILS_TITLE';
    }
    return this.store.isUpdate() ? 'STATUS_DEFINITIONS.FORM.UPDATE_TITLE' : 'STATUS_DEFINITIONS.FORM.CREATE_TITLE';
  });
  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'FORMS.INVALID';
    }
    return this.hasNoChanges() ? 'FORMS.NO_CHANGES' : null;
  });

  constructor() {
    effect(() => {
      this.store.start(this.machineId(), this.statusDefinitionId());
    });
  }

  protected onEdit(): void {
    this.store.edit();
    this._current.set(null);
    this._isValid.set(false);
  }

  protected onValueChange(value: StatusDefinitionFormValue): void {
    this._current.set(value);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveDisabledReason() === null) {
      this.store.save({ value, onSaved: () => (this.store.isUpdate() ? this.updated.emit() : this.saved.emit()) });
    }
  }
}
