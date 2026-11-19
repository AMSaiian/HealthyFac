import { Component, computed, inject, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { hasFormChanges } from '@shared/forms/form-changes';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { OperatingPhasesPageStore } from '@machines/phases/operating-phases-page-store/operating-phases-page.store';
import { PhaseDetails } from '@machines/phases/phase-details/phase-details';
import { PhaseForm } from '@machines/phases/phase-form/phase-form';
import { phaseFormSchema, type PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import { EMPTY_FORM_VALUE, toFormValue } from '@machines/phases/phase-form/phase-form.utils';

/** The action host of the phase dialog. It reads the page store and starts the save. */
@Component({
  imports: [ActionDialog, LoadingSpinner, PhaseDetails, PhaseForm],
  host: { class: 'contents' },
  selector: 'hf-phase-dialog-host',
  templateUrl: './phase-dialog-host.html',
})
export class PhaseDialogHost {
  protected readonly store = inject(OperatingPhasesPageStore);

  protected readonly initialValue = computed((): PhaseFormValue | null => {
    if (this.store.editor()?.phaseId === null) {
      return EMPTY_FORM_VALUE;
    }
    const phase = this.store.editedPhase();
    return phase === null ? null : toFormValue(phase);
  });
  protected readonly isNew = computed(() => this.store.editor()?.phaseId === null);
  protected readonly title = computed((): string => {
    if (this.store.isViewMode()) {
      return 'PHASES.FORM.DETAILS_TITLE';
    }
    return this.isNew() ? 'PHASES.FORM.CREATE_TITLE' : 'PHASES.FORM.UPDATE_TITLE';
  });
  protected readonly isReady = computed(
    () => this.initialValue() !== null && this.store.metrics.hasValue() && this.store.statuses.hasValue(),
  );
  protected readonly showSpinner = delayedLoading(computed(() => !this.isReady()));
  protected readonly showSaving = delayedLoading(this.store.isSaving);
  protected readonly metricList = computed(() =>
    this.store.metrics.hasValue() ? this.store.metrics.value().definitions : [],
  );

  private readonly _current = signal<PhaseFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'FORMS.INVALID';
    }
    const initial = this.initialValue();
    const current = this._current();
    const hasNoChanges =
      !this.isNew() && initial !== null && current !== null && !hasFormChanges(phaseFormSchema, initial, current);
    return hasNoChanges ? 'FORMS.NO_CHANGES' : null;
  });

  protected onEdit(): void {
    this.store.edit();
    this._current.set(null);
    this._isValid.set(false);
  }

  protected onValueChange(value: PhaseFormValue): void {
    this._current.set(value);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveDisabledReason() === null) {
      this.store.save({ value, onSaved: () => this.store.onSaved() });
    }
  }
}
