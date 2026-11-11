import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Checkbox } from 'primeng/checkbox';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';

import { DraftsApi } from '@machines/drafts/drafts-api';
import { StatusDraftReviewStore } from '@machines/drafts/status/status-draft-review.store';
import { StatusCompareField, type StatusCompareRow } from '@machines/drafts/status/status-draft-review.store.utils';
import { StatusDefinitionForm } from '@machines/statuses/drawer/status-definition-form/status-definition-form';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';
import type { StatusSeverity } from '@machines/statuses/status-definition';
import { SEVERITY_LABEL_KEYS } from '@machines/statuses/status-definition.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

@Component({
  imports: [ActionDialog, Checkbox, FormsModule, LoadingSpinner, RichTextView, StatusDefinitionForm, TranslatePipe],
  host: { class: 'contents' },
  providers: [DraftsApi, StatusDefinitionsApi, StatusDraftReviewStore],
  selector: 'hf-status-draft-review-host',
  templateUrl: './status-draft-review-host.html',
})
export class StatusDraftReviewHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(StatusDraftReviewStore);
  protected readonly StatusCompareField = StatusCompareField;

  private readonly _current = signal<StatusDefinitionFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly title = computed(() =>
    this.store.mode() === 'update' ? 'DRAFTS.STATUS.UPDATE_TITLE' : 'DRAFTS.STATUS.CREATE_TITLE',
  );
  protected readonly saveDisabledReason = computed((): string | null => {
    switch (this.store.mode()) {
      case 'create':
        return this._isValid() ? null : 'STATUS_DEFINITIONS.FORM.INVALID';
      case 'update':
        return this.store.updateDisabledReason();
      default:
        return 'DRAFTS.STATUS.NOT_READY';
    }
  });

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }

  protected severityKey(severity: string | null): string {
    return severity === null ? '' : SEVERITY_LABEL_KEYS[severity as StatusSeverity];
  }

  protected labelKey(row: StatusCompareRow): string {
    return row.field === StatusCompareField.Severity ? 'DRAFTS.STATUS.SEVERITY' : 'DRAFTS.STATUS.DESCRIPTION';
  }

  protected onValueChange(value: StatusDefinitionFormValue): void {
    this._current.set(value);
  }

  protected onValidityChange(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    if (this.saveDisabledReason() !== null) {
      return;
    }
    if (this.store.mode() === 'create') {
      const value = this._current();
      if (value !== null) {
        this.store.saveCreate(value);
      }
    } else {
      this.store.saveUpdate();
    }
  }
}
