import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Textarea } from 'primeng/textarea';
import { maxLength, minLength, object, string, trim } from 'zod/mini';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { isFieldInvalid } from '@shared/forms/field-state';

const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';

/** The longest close reason. It mirrors `CaseLimits.CloseReasonMaxLength` of the backend. */
export const CLOSE_REASON_MAX_LENGTH = 500;

export const closeCaseSchema = object({
  reason: string().check(
    trim(),
    minLength(1, REQUIRED_ERROR_KEY),
    maxLength(CLOSE_REASON_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  ),
});

/** The dialog that asks for the reason before a case closes. It reads no store. */
@Component({
  imports: [ActionDialog, FormField, Textarea, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-close-case-dialog',
  templateUrl: './close-case-dialog.html',
})
export class CloseCaseDialog {
  readonly isSaving = input(false);

  readonly closeRequested = output<string>();
  readonly cancelled = output<void>();

  protected readonly model = linkedSignal(() => ({ reason: '' }));
  protected readonly closeForm = form(this.model, (path) => {
    validateStandardSchema(path, closeCaseSchema);
  });
  protected readonly saveDisabledReason = computed((): string | null =>
    this.closeForm().valid() ? null : 'FORMS.INVALID',
  );
  protected readonly isShown = computed(() =>
    isFieldInvalid({ isInvalid: this.closeForm.reason().invalid(), isTouched: this.closeForm.reason().touched() }),
  );

  protected onSave(): void {
    if (this.saveDisabledReason() === null) {
      this.closeRequested.emit(this.model().reason.trim());
    }
  }
}
