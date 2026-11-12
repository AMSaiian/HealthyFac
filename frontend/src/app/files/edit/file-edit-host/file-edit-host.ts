import { Component, computed, effect, inject, input, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { hasFormChanges } from '@shared/forms/form-changes';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { FileEditForm } from '@files/edit/file-edit-form/file-edit-form';
import { fileEditFormSchema, type FileEditFormValue } from '@files/edit/file-edit-form/file-edit-form-schema';
import { FileEditStore } from '@files/edit/file-edit.store';
import { FilesApi } from '@files/files-api';

@Component({
  imports: [ActionDialog, FileEditForm, LoadingSpinner],
  host: { class: 'contents' },
  providers: [FilesApi, FileEditStore],
  selector: 'hf-file-edit-host',
  templateUrl: './file-edit-host.html',
})
export class FileEditHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(FileEditStore);
  protected readonly showSpinner = delayedLoading(computed(() => !this.store.initialValue()));

  private readonly _current = signal<FileEditFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly saveDisabledReason = computed((): string | null => {
    if (!this._isValid()) {
      return 'FORMS.INVALID';
    }
    const initial = this.store.initialValue();
    const current = this._current();
    return initial !== null && current !== null && !hasFormChanges(fileEditFormSchema, initial, current)
      ? 'FORMS.NO_CHANGES'
      : null;
  });

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }

  protected onValueChange(value: FileEditFormValue): void {
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
