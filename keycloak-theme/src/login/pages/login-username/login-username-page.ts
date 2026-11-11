import { Component, inject, signal, type TemplateRef, viewChild } from '@angular/core';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { ButtonDirective } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import type { I18n } from '../../i18n';

@Component({
  selector: 'hf-login-username-page',
  imports: [CheckboxModule, FormsModule, InputTextModule, ButtonDirective, MessageModule, KcSanitizePipe],
  templateUrl: './login-username-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class LoginUsernamePage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'login-username.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = !this.kcContext.messagesPerField.existsError('username');
  protected readonly isSubmitDisabled = signal(false);

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');
}
