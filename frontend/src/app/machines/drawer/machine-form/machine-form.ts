import { Component, computed, effect, input, linkedSignal, output } from '@angular/core';
import { disabled, type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import type { DepartmentOption } from '@shared/employee/employee-api';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import { isFieldInvalid } from '@shared/forms/field-state';
import { machineFormSchema, type MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';
import { DESCRIPTION_MAX_LENGTH } from '@machines/machine-limits';

@Component({
  imports: [ButtonDirective, FormField, InputText, RichTextEditor, Select, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-machine-form',
  templateUrl: './machine-form.html',
})
export class MachineForm {
  readonly initialValue = input.required<MachineFormValue>();
  readonly departments = input.required<readonly DepartmentOption[]>();
  readonly valueChange = output<MachineFormValue>();
  readonly validityChange = output<boolean>();
  readonly isDepartmentsLoading = input(false);
  readonly isUpdate = input(false);
  readonly gatewayId = input<string | null>(null);

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  protected readonly model = linkedSignal<MachineFormValue>(() => ({ ...this.initialValue() }));
  protected readonly machineForm = form(this.model, (path) => {
    validateStandardSchema(path, machineFormSchema);
    disabled(path.departmentId, { when: () => this.isUpdate() });
  });
  protected readonly departmentOptions = computed(() => [...this.departments()]);

  protected copyGatewayId(): void {
    const gatewayId = this.gatewayId();
    if (gatewayId !== null) {
      void navigator.clipboard.writeText(gatewayId);
    }
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.machineForm().valid());
    });
  }
}
