import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { isFieldInvalid } from '@shared/forms/field-state';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';

import type { ApplyWorkOrderDraftBody, WorkOrderDraft } from '@work-orders/drafts/work-order-draft';
import { toApplyBody, toFormValue } from '@work-orders/drafts/draft-apply-dialog/draft-apply-dialog.utils';
import { CATEGORY_LABEL_KEYS, PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import {
  DESCRIPTION_MAX_LENGTH,
  ESTIMATE_MAX_MINUTES,
  ESTIMATE_MIN_MINUTES,
  ESTIMATE_STEP_MINUTES,
} from '@work-orders/work-order-limits';
import { workOrderFormSchema, type WorkOrderFormValue } from '@work-orders/work-order-schema';

/** The dialog that edits a work order draft and applies it. It reads no store. */
@Component({
  imports: [ActionDialog, FormField, InputNumber, InputText, RichTextEditor, Select, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-draft-apply-dialog',
  templateUrl: './draft-apply-dialog.html',
})
export class DraftApplyDialog {
  readonly draft = input.required<WorkOrderDraft>();
  readonly isSaving = input(false);

  readonly applyRequested = output<ApplyWorkOrderDraftBody>();
  readonly cancelled = output<void>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly estimateMin = ESTIMATE_MIN_MINUTES;
  protected readonly estimateMax = ESTIMATE_MAX_MINUTES;
  protected readonly estimateStep = ESTIMATE_STEP_MINUTES;
  private readonly _labels = toSignal(
    inject(TranslateService).stream([...Object.values(PRIORITY_LABEL_KEYS), ...Object.values(CATEGORY_LABEL_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );

  /** The options carry the translated `label`, because PrimeNG builds the accessible name from the option label. */
  protected readonly priorityOptions = computed(() =>
    Object.values(WorkOrderPriority).map((value) => ({
      value,
      label: this._labels()[PRIORITY_LABEL_KEYS[value]] ?? PRIORITY_LABEL_KEYS[value],
    })),
  );
  protected readonly categoryOptions = computed(() =>
    Object.values(WorkOrderCategory).map((value) => ({
      value,
      label: this._labels()[CATEGORY_LABEL_KEYS[value]] ?? CATEGORY_LABEL_KEYS[value],
    })),
  );

  protected readonly model = linkedSignal<WorkOrderFormValue>(() => toFormValue(this.draft()));
  protected readonly applyForm = form(this.model, (path) => {
    validateStandardSchema(path, workOrderFormSchema);
  });
  protected readonly saveDisabledReason = computed((): string | null =>
    this.applyForm().valid() ? null : 'FORMS.INVALID',
  );

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  protected onSave(): void {
    if (this.saveDisabledReason() === null) {
      this.applyRequested.emit(toApplyBody(this.draft(), this.model()));
    }
  }
}
