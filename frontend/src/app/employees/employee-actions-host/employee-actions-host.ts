import { Component, computed, effect, inject, input, signal } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { EmployeeRole } from '@shared/employee/employee';

import { EmployeeActionsStore } from '@employees/employee-actions-store/employee-actions.store';
import { EmployeesApi } from '@employees/employees-api';
import { InviteDialog } from '@employees/invites/invite-dialog/invite-dialog';
import type { InviteFormValue } from '@employees/invites/invite-dialog/invite-dialog-schema';
import { inviteSaveReason, type SaveReason } from '@employees/invites/invite-dialog/invite-dialog.utils';

const INITIAL_INVITE: InviteFormValue = {
  email: '',
  roleCode: EmployeeRole.Worker,
  firstName: '',
  lastName: '',
};

@Component({
  imports: [ActionDialog, InviteDialog],
  host: { class: 'contents' },
  providers: [EmployeesApi, EmployeeActionsStore],
  selector: 'hf-employee-actions-host',
  templateUrl: './employee-actions-host.html',
})
export class EmployeeActionsHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(EmployeeActionsStore);
  protected readonly initialInvite = INITIAL_INVITE;

  private readonly _current = signal<InviteFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly saveReason = computed((): SaveReason | null => inviteSaveReason(this._current(), this._isValid()));

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }

  protected onValue(value: InviteFormValue): void {
    this._current.set(value);
  }

  protected onValidity(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected onSave(): void {
    const value = this._current();
    if (value !== null && this.saveReason() === null) {
      this.store.submitInvite(value);
    }
  }
}
