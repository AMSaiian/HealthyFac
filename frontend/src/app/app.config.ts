import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeUk from '@angular/common/locales/uk';
import type { ApplicationConfig } from '@angular/core';
import { ErrorHandler, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy, withAutoCleanupInjectors, withComponentInputBinding } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';

import { routes } from '@app/app.routes';
import { APP_CONFIG, type AppConfig } from '@shared/config/app-config';
import { FileActionType } from '@files/file-actions';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachineActionType } from '@machines/machine-actions';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { EmployeeActionType } from '@employees/employee-actions';
import { provideActionHosts } from '@shared/actions/action-registry';
import { AppErrorHandler } from '@shared/errors/app-error-handler';
import { apiBaseUrlInterceptor } from '@shared/http/api-base-url.interceptor';
import { problemDetailsInterceptor } from '@shared/http/problem-details.interceptor';
import { DEFAULT_LANGUAGE, TRANSLATION_PATH, TRANSLATION_SUFFIX } from '@shared/i18n/i18n.constants';
import { TranslatedTitleStrategy } from '@shared/i18n/translated-title-strategy';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { authInterceptor } from '@shared/security/auth.interceptor';
import { readCspNonce } from '@shared/security/csp-nonce';
import { HealthyFacPreset } from '@shared/theme/healthyfac-preset';
import { DARK_MODE_CLASS } from '@shared/theme/theme.constants';

registerLocaleData(localeUk);

/**
 * Builds the providers from the runtime config, because PrimeNG reads its license key only once, at start.
 */
export function createAppConfig(config: AppConfig): ApplicationConfig {
  return {
    providers: [
      { provide: APP_CONFIG, useValue: config },
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes, withComponentInputBinding(), withAutoCleanupInjectors()),
      provideAppInitializer(() => inject(AuthStore).init()),
      provideHttpClient(withInterceptors([apiBaseUrlInterceptor, problemDetailsInterceptor, authInterceptor])),
      provideTranslateService({
        fallbackLang: DEFAULT_LANGUAGE,
        loader: provideTranslateHttpLoader({ prefix: TRANSLATION_PATH, suffix: TRANSLATION_SUFFIX }),
      }),
      providePrimeNG({
        license: config.primeUiLicenseKey,
        csp: { nonce: readCspNonce(document.querySelector('hf-root')) },
        theme: {
          preset: HealthyFacPreset,
          options: {
            darkModeSelector: `.${DARK_MODE_CLASS}`,
            cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
          },
        },
      }),
      provideActionHosts([
        ...[MachineActionType.Create, MachineActionType.Update].map((type) => ({
          type,
          load: async () => {
            const [host, form] = await Promise.all([
              import('@machines/drawer/machine-drawer-host/machine-drawer-host'),
              import('@machines/drawer/machine-form/machine-form-schema'),
            ]);
            return { component: host.MachineDrawerHost, schema: form.machineActionPayloadSchema };
          },
        })),
        {
          type: WorkOrderActionType.Open,
          load: async () => {
            const [host, schema] = await Promise.all([
              import('@work-orders/drawer/work-order-drawer-host/work-order-drawer-host'),
              import('@work-orders/work-order-schema'),
            ]);
            return { component: host.WorkOrderDrawerHost, schema: schema.workOrderActionPayloadSchema };
          },
        },
        ...[
          WorkOrderActionType.Assign,
          WorkOrderActionType.Move,
          WorkOrderActionType.Unassign,
          WorkOrderActionType.ChangeStatus,
          WorkOrderActionType.Delete,
        ].map((type) => ({
          type,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@work-orders/schedule-actions/schedule-actions-host/schedule-actions-host'),
              import('@work-orders/work-order-actions'),
            ]);
            return { component: host.ScheduleActionsHost, schema: payloads.workOrderRowActionPayloadSchema };
          },
        })),
        ...[
          EmployeeActionType.Invite,
          EmployeeActionType.Resend,
          EmployeeActionType.RetryProvisioning,
          EmployeeActionType.Revoke,
        ].map((type) => ({
          type,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@employees/employee-actions-host/employee-actions-host'),
              import('@employees/employee-action-payloads'),
            ]);
            return { component: host.EmployeeActionsHost, schema: payloads.employeeActionPayloadSchema };
          },
        })),
        {
          type: FileActionType.Edit,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@files/edit/file-edit-host/file-edit-host'),
              import('@files/file-action-payloads'),
            ]);
            return { component: host.FileEditHost, schema: payloads.fileEditActionPayloadSchema };
          },
        },
        {
          type: MachineActionType.Drafts,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@machines/drafts/list/machine-drafts-host/machine-drafts-host'),
              import('@machines/drafts/draft-action-payloads'),
            ]);
            return { component: host.MachineDraftsHost, schema: payloads.machineDraftsActionPayloadSchema };
          },
        },
        {
          type: DraftActionType.CompareMachine,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@machines/drafts/compare/draft-compare-host/draft-compare-host'),
              import('@machines/drafts/draft-action-payloads'),
            ]);
            return { component: host.DraftCompareHost, schema: payloads.draftCompareActionPayloadSchema };
          },
        },
        {
          type: DraftActionType.ReviewStatus,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@machines/drafts/status/status-draft-review-host/status-draft-review-host'),
              import('@machines/drafts/draft-action-payloads'),
            ]);
            return { component: host.StatusDraftReviewHost, schema: payloads.statusDraftReviewActionPayloadSchema };
          },
        },
        {
          type: FileActionType.Tie,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@files/tie/file-tie-host/file-tie-host'),
              import('@files/file-action-payloads'),
            ]);
            return { component: host.FileTieHost, schema: payloads.fileTieActionPayloadSchema };
          },
        },
        {
          type: FileActionType.IngestHistory,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@files/ingest/history/ingest-history-host/ingest-history-host'),
              import('@files/file-action-payloads'),
            ]);
            return { component: host.IngestHistoryHost, schema: payloads.ingestHistoryActionPayloadSchema };
          },
        },
        {
          type: FileActionType.IngestSummary,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@files/ingest/summary/ingest-summary-host/ingest-summary-host'),
              import('@files/file-action-payloads'),
            ]);
            return { component: host.IngestSummaryHost, schema: payloads.ingestSummaryActionPayloadSchema };
          },
        },
        {
          type: FileActionType.Upload,
          load: async () => {
            const [host, payloads] = await Promise.all([
              import('@files/upload/file-upload-host/file-upload-host'),
              import('@files/file-action-payloads'),
            ]);
            return { component: host.FileUploadHost, schema: payloads.fileUploadActionPayloadSchema };
          },
        },
      ]),
      MessageService,
      ConfirmationService,
      { provide: ErrorHandler, useClass: AppErrorHandler },
      { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
    ],
  };
}
