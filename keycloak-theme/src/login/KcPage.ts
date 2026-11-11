import type { Type } from '@angular/core';
import { getDefaultPageComponent, type KcPage } from '@keycloakify/angular/login';
import { UserProfileFormFieldsComponent } from '@keycloakify/angular/login/components/user-profile-form-fields';
import type { ClassKey } from 'keycloakify/login';
import type { KcContext } from './KcContext';
import { TemplateComponent } from './template/template';

export const classes = {} satisfies Partial<Record<ClassKey, string>>;
export const doUseDefaultCss = false;
export const doMakeUserConfirmPassword = true;

async function loadPageComponent(pageId: KcContext['pageId']): Promise<Type<unknown>> {
  switch (pageId) {
    case 'login.ftl':
      return (await import('./pages/login/login-page')).LoginPage;
    case 'login-username.ftl':
      return (await import('./pages/login-username/login-username-page')).LoginUsernamePage;
    case 'login-password.ftl':
      return (await import('./pages/login-password/login-password-page')).LoginPasswordPage;
    case 'login-update-password.ftl':
      return (await import('./pages/login-update-password/login-update-password-page')).LoginUpdatePasswordPage;
    case 'login-reset-password.ftl':
      return (await import('./pages/login-reset-password/login-reset-password-page')).LoginResetPasswordPage;
    case 'login-page-expired.ftl':
      return (await import('./pages/login-page-expired/login-page-expired-page')).LoginPageExpiredPage;
    case 'error.ftl':
      return (await import('./pages/error/error-page')).ErrorPage;
    case 'info.ftl':
      return (await import('./pages/info/info-page')).InfoPage;
    case 'logout-confirm.ftl':
      return (await import('./pages/logout-confirm/logout-confirm-page')).LogoutConfirmPage;
    default:
      return getDefaultPageComponent(pageId);
  }
}

export async function getKcPage(pageId: KcContext['pageId']): Promise<KcPage> {
  return {
    PageComponent: await loadPageComponent(pageId),
    TemplateComponent,
    UserProfileFormFieldsComponent,
    doMakeUserConfirmPassword,
    doUseDefaultCss,
    classes,
  };
}
