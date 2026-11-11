import { Component, computed, effect, inject, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import { EmployeeRole } from '@shared/employee/employee';
import { isFieldInvalid } from '@shared/forms/field-state';

import { inviteFormSchema, type InviteFormValue } from '@employees/invites/invite-dialog/invite-dialog-schema';

const ROLE_KEYS: Readonly<Record<EmployeeRole, string>> = {
  [EmployeeRole.Worker]: 'EMPLOYEES.ROLES.WORKER',
  [EmployeeRole.Supervisor]: 'EMPLOYEES.ROLES.SUPERVISOR',
};

/** The form of an invite: the email and the role of the new employee. */
@Component({
  imports: [FormField, InputText, Select, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-invite-dialog',
  templateUrl: './invite-dialog.html',
})
export class InviteDialog {
  readonly initialValue = input.required<InviteFormValue>();

  readonly valueChange = output<InviteFormValue>();
  readonly validityChange = output<boolean>();

  private readonly _roleLabels = toSignal(inject(TranslateService).stream(Object.values(ROLE_KEYS)), {
    initialValue: {} as Record<string, string>,
  });

  protected readonly roleOptions = computed(() =>
    Object.values(EmployeeRole).map((value) => ({ value, label: this._roleLabels()[ROLE_KEYS[value]] ?? value })),
  );

  protected readonly model = linkedSignal<InviteFormValue>(() => ({ ...this.initialValue() }));
  protected readonly inviteForm = form(this.model, (path) => {
    validateStandardSchema(path, inviteFormSchema);
  });

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.inviteForm().valid());
    });
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }
}
