import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';

import { isFieldInvalid } from '@shared/forms/field-state';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import { DESCRIPTION_MAX_LENGTH } from '@machines/error-codes/machine-error-code-limits';
import {
  machineErrorCodeFormSchema,
  type MachineErrorCodeFormValue,
} from '@machines/error-codes/drawer/machine-error-code-form-schema';

@Component({
  imports: [FormField, InputText, RichTextEditor, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-machine-error-code-form',
  templateUrl: './machine-error-code-form.html',
})
export class MachineErrorCodeForm {
  readonly initialValue = input.required<MachineErrorCodeFormValue>();
  readonly valueChange = output<MachineErrorCodeFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  protected readonly model = linkedSignal<MachineErrorCodeFormValue>(() => ({ ...this.initialValue() }));
  protected readonly errorCodeForm = form(this.model, (path) => {
    validateStandardSchema(path, machineErrorCodeFormSchema);
  });

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.errorCodeForm().valid());
    });
  }
}
