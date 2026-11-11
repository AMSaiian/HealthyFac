import { Component, computed, inject, type TemplateRef, viewChild } from '@angular/core';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { ButtonDirective } from 'primeng/button';
import type { I18n } from '../../i18n';
import { requiredActionKeys, summaryToShow, titleOf } from './info-page.utils';

@Component({
  selector: 'hf-info-page',
  imports: [ButtonDirective, KcSanitizePipe],
  templateUrl: './info-page.html',
  host: { class: 'flex flex-col gap-4' },
})
export class InfoPage implements TemplateSlots {
  protected readonly kcContext = inject<Extract<KcContext, { pageId: 'info.ftl' }>>(KC_LOGIN_CONTEXT);
  protected readonly i18n = inject<I18n>(LOGIN_I18N);

  displayInfo = false;
  displayMessage = false;

  readonly headerNode = viewChild<TemplateRef<HTMLElement>>('headerNode');

  protected readonly title = computed(() =>
    titleOf(this.kcContext.messageHeader, this.kcContext.message.summary, (key) => this.i18n.advancedMsgStr(key)),
  );
  protected readonly summary = computed(() => summaryToShow(this.title(), this.kcContext.message.summary));
  protected readonly actionNames = computed(() =>
    requiredActionKeys(this.kcContext.requiredActions).map((key) => this.i18n.advancedMsgStr(key)),
  );
}
