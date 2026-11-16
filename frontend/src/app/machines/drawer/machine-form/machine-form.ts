import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validate, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';

import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import { isFieldInvalid } from '@shared/forms/field-state';
import { PlainDatePicker } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import type { Temporal } from '@shared/time/temporal';
import { MachineState } from '@machines/machine';
import { STATE_LABEL_KEYS } from '@machines/list/machine-grid/machine-grid.utils';
import {
  machineFormSchema,
  type MachineFormValue,
  REQUIRED_ERROR_KEY,
} from '@machines/drawer/machine-form/machine-form-schema';
import { DESCRIPTION_MAX_LENGTH } from '@machines/machine-limits';

@Component({
  imports: [ButtonDirective, FormField, InputText, PlainDatePicker, RichTextEditor, Select, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-machine-form',
  templateUrl: './machine-form.html',
})
export class MachineForm {
  readonly initialValue = input.required<MachineFormValue>();
  readonly valueChange = output<MachineFormValue>();
  readonly validityChange = output<boolean>();
  readonly isUpdate = input(false);
  readonly gatewayId = input<string | null>(null);
  readonly minGoLiveOn = input<Temporal.PlainDate | undefined>(undefined);

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly stateOptions = Object.values(MachineState).map((value) => ({
    value,
    labelKey: STATE_LABEL_KEYS[value],
  }));

  protected readonly model = linkedSignal<MachineFormValue>(() => ({ ...this.initialValue() }));
  protected readonly machineForm = form(this.model, (path) => {
    validateStandardSchema(path, machineFormSchema);
    validate(path.state, (context) =>
      this.isUpdate() && context.value() === null ? { kind: 'required', message: REQUIRED_ERROR_KEY } : undefined,
    );
  });

  protected copyGatewayId(): void {
    const gatewayId = this.gatewayId();
    if (gatewayId !== null) {
      void navigator.clipboard.writeText(gatewayId);
    }
  }

  protected setGoLiveOn(date: Temporal.PlainDate | null): void {
    this.model.update((value) => ({ ...value, expectedGoLiveOn: date }));
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
