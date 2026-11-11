import { i18nBuilder } from '@keycloakify/angular/login';
import type { ThemeName } from '../kc.gen';

const i18n = i18nBuilder
  .withThemeName<ThemeName>()
  .withExtraLanguages({})
  .withCustomTranslations({
    en: {
      brandTitle: 'HealthyFac',
      proceed: 'Continue',
      accountUpdatedTitle: 'Account updated',
      backToApplication: 'Back to application',
      setPasswordHint: 'Choose a new password. It needs at least 12 characters and must differ from your email',
    },
    uk: {
      brandTitle: 'HealthyFac',
      proceed: 'Продовжити',
      accountUpdatedTitle: 'Обліковий запис оновлено',
      backToApplication: 'Назад до застосунку',
      setPasswordHint: 'Придумайте новий пароль. Він має містити щонайменше 12 символів і відрізнятися від вашої пошти',
    },
  })
  .build();

export const getI18n = i18n.getI18n;

export type I18n = typeof i18n.ofTypeI18n;
