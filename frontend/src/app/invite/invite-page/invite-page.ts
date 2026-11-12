import { Component, computed, effect, inject, signal } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Tooltip } from 'primeng/tooltip';

import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { isFieldInvalid } from '@shared/forms/field-state';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { InviteStore } from '@invite/invite.store';
import { acceptFormSchema, type AcceptFormValue } from '@invite/invite-page/invite-page-schema';

const ROLE_KEYS: Readonly<Record<string, string>> = {
  worker: 'EMPLOYEES.ROLES.WORKER',
  supervisor: 'EMPLOYEES.ROLES.SUPERVISOR',
};

@Component({
  imports: [ButtonDirective, FormField, InputText, LoadingSpinner, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 flex-1 items-center justify-center p-4' },
  selector: 'hf-invite-page',
  templateUrl: './invite-page.html',
})
export class InvitePage {
  protected readonly store = inject(InviteStore);
  protected readonly showSpinner = delayedLoading(computed(() => this.store.view() === 'loading'));

  private readonly _auth = inject(AuthStore);

  protected readonly signedInEmail = this._auth.email;

  protected readonly roleKey = computed(() => ROLE_KEYS[this.store.details()?.roleCode ?? ''] ?? '');
  protected readonly model = signal<AcceptFormValue>({ firstName: '', lastName: '' });
  protected readonly acceptForm = form(this.model, (path) => {
    validateStandardSchema(path, acceptFormSchema);
  });

  constructor() {
    effect(() => {
      const details = this.store.details();
      if (details !== null) {
        this.model.set({ firstName: details.firstName ?? '', lastName: details.lastName ?? '' });
      }
    });
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  protected submit(): void {
    if (this.acceptForm().valid() && !this.store.isSaving()) {
      this.store.accept({ firstName: this.model().firstName.trim(), lastName: this.model().lastName.trim() });
    }
  }

  protected signOutAndContinue(): void {
    void this.store.signOutAndContinue();
  }

  protected signIn(): void {
    void this._auth.login('/');
  }
}
