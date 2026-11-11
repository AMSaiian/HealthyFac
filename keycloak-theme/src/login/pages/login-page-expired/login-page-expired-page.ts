import { Component, inject, type TemplateRef, viewChild } from '@angular/core';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { ButtonDirective } from 'primeng/button';
import type { I18n } from '../../i18n';

@Component({
  selector: 'hf-login-page-expired-page',
  imports: [ButtonDirective],
  templateUrl: './login-page-expired-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class LoginPageExpiredPage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'login-page-expired.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = false;

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');
}
