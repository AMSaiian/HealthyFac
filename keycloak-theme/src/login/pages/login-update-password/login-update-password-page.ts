import { Component, inject, type TemplateRef, viewChild } from '@angular/core';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordField } from '../../password-field/password-field';
import { ButtonDirective } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import type { I18n } from '../../i18n';

@Component({
  selector: 'hf-login-update-password-page',
  imports: [
    PasswordField,
    CheckboxModule,
    FormsModule,
    InputTextModule,
    ButtonDirective,
    MessageModule,
    KcSanitizePipe,
  ],
  templateUrl: './login-update-password-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class LoginUpdatePasswordPage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'login-update-password.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = !this.kcContext.messagesPerField.existsError('password', 'password-confirm');

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');
}
