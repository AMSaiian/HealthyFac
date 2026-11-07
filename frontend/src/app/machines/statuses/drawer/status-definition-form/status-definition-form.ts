import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { isFieldInvalid } from '@shared/forms/field-state';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import { SEVERITY_OPTIONS } from '@machines/statuses/status-definition.utils';
import { DESCRIPTION_MAX_LENGTH } from '@machines/statuses/status-definition-limits';
import {
  statusDefinitionFormSchema,
  type StatusDefinitionFormValue,
} from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';

@Component({
  imports: [FormField, InputText, RichTextEditor, Select, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-status-definition-form',
  templateUrl: './status-definition-form.html',
})
export class StatusDefinitionForm {
  readonly initialValue = input.required<StatusDefinitionFormValue>();
  readonly valueChange = output<StatusDefinitionFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly severityOptions = SEVERITY_OPTIONS;

  protected readonly model = linkedSignal<StatusDefinitionFormValue>(() => ({ ...this.initialValue() }));
  protected readonly statusDefinitionForm = form(this.model, (path) => {
    validateStandardSchema(path, statusDefinitionFormSchema);
  });

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.statusDefinitionForm().valid());
    });
  }
}
