import { Component, computed, input, signal } from '@angular/core';
import { ButtonDirective } from 'primeng/button';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { InputPasswordModule } from 'primeng/inputpassword';
import { TooltipModule } from 'primeng/tooltip';

/** A native password input for the Keycloak form post, with a keyboard-operable show and hide button. */
@Component({
  selector: 'hf-password-field',
  imports: [ButtonDirective, InputGroupModule, InputGroupAddonModule, InputPasswordModule, TooltipModule],
  templateUrl: './password-field.html',
  host: { class: 'block' },
})
export class PasswordField {
  readonly inputId = input.required<string>();
  readonly name = input.required<string>();
  readonly autocomplete = input.required<string>();
  readonly showLabel = input.required<string>();
  readonly hideLabel = input.required<string>();
  readonly isInvalid = input(false);
  readonly isRequired = input(false);

  protected readonly isMasked = signal(true);
  protected readonly toggleLabel = computed(() => (this.isMasked() ? this.showLabel() : this.hideLabel()));
}
