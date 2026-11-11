import { Component, inject, signal, type TemplateRef, viewChild } from '@angular/core';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { ButtonDirective } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { PasswordField } from '../../password-field/password-field';
import { TooltipModule } from 'primeng/tooltip';
import { MessageModule } from 'primeng/message';
import type { I18n } from '../../i18n';

@Component({
  selector: 'hf-login-password-page',
  imports: [
    InputGroupAddonModule,
    InputGroupModule,
    PasswordField,
    TooltipModule,
    InputTextModule,
    ButtonDirective,
    MessageModule,
    KcSanitizePipe,
  ],
  templateUrl: './login-password-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class LoginPasswordPage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'login-password.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = !this.kcContext.messagesPerField.existsError('password');
  protected readonly isSubmitDisabled = signal(false);

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');
}
