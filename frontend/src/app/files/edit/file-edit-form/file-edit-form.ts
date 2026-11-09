import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';

import { isFieldInvalid } from '@shared/forms/field-state';

import { fileEditFormSchema, type FileEditFormValue } from './file-edit-form-schema';

@Component({
  imports: [FormField, InputText, Textarea, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-file-edit-form',
  templateUrl: './file-edit-form.html',
})
export class FileEditForm {
  readonly initialValue = input.required<FileEditFormValue>();
  readonly extension = input.required<string>();
  readonly valueChange = output<FileEditFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly model = linkedSignal<FileEditFormValue>(() => ({ ...this.initialValue() }));
  protected readonly editForm = form(this.model, (path) => {
    validateStandardSchema(path, fileEditFormSchema);
  });

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.editForm().valid());
    });
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }
}
