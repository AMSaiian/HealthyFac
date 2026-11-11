import { Component, inject, type TemplateRef, viewChild } from '@angular/core';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { ButtonDirective } from 'primeng/button';
import type { I18n } from '../../i18n';

@Component({
  selector: 'hf-logout-confirm-page',
  imports: [ButtonDirective, KcSanitizePipe],
  templateUrl: './logout-confirm-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class LogoutConfirmPage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'logout-confirm.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = true;

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');
}
