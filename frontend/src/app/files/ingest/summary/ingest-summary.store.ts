import { HttpContext, HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { EMPTY, exhaustMap, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';

import type { IngestSummaryActionPayload } from '@files/file-action-payloads';
import { IngestApi } from '@files/ingest/ingest-api';
import { attemptReasonKey, parseWarning } from '@files/ingest/ingest-attempt.utils';
import { type DraftRow, toDraftGroups } from '@files/ingest/summary/ingest-summary.store.utils';
import { DraftKind } from '@machines/drafts/draft';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { reviewMachineDraft, reviewMetricDrafts, reviewStatusDraft } from '@machines/drafts/draft-review';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { withDraftQuickApply } from '@machines/drafts/quick-apply/with-draft-quick-apply';

interface SummaryState {
  action: AppAction | null;
  /** Set when a draft was dismissed, so the owner of the lists reloads after the dialog closes. */
  hasChanged: boolean;
}

function isNotFound(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound;
}

export const IngestSummaryStore = signalStore(
  withState<SummaryState>({ action: null, hasChanged: false }),
  withProps(() => ({
    _ingestApi: inject(IngestApi),
    _draftsApi: inject(DraftsApi),
    _actions: inject(ActionStore),
    _auth: inject(AuthStore),
    _router: inject(Router),
    _confirmation: inject(ConfirmationService),
    _messages: inject(MessageService),
    _translate: inject(TranslateService),
  })),
  withComputed((store) => ({
    attemptId: computed(() => (store.action()?.payload as IngestSummaryActionPayload | undefined)?.attemptId),
  })),
  withProps((store) => ({
    attempt: rxResource({
      params: () => store.attemptId(),
      stream: ({ params }) => store._ingestApi.get(params, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }),
    }),
    drafts: rxResource({
      params: () => store.attemptId(),
      stream: ({ params }) =>
        store._draftsApi.listForAttempt(params, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }),
    }),
  })),
  withComputed((store) => ({
    isLoading: computed(() => store.attempt.isLoading() || store.drafts.isLoading()),
    isNotFound: computed(() => isNotFound(store.attempt.error()) || isNotFound(store.drafts.error())),
    canWrite: computed(() => store._auth.hasPermission(Permission.DraftsWrite)),
    reasonKey: computed(() => (store.attempt.hasValue() ? attemptReasonKey(store.attempt.value()) : null)),
    warning: computed(() => (store.attempt.hasValue() ? parseWarning(store.attempt.value().warning) : null)),
    groups: computed(() => (store.drafts.hasValue() ? toDraftGroups(store.drafts.value().groups) : [])),
  })),
  withDraftQuickApply(),
  withMethods((store) => {
    const dismissDraft = rxMethod<DraftRow>(
      pipe(
        exhaustMap((row) =>
          store._draftsApi.dismiss(row.id, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).pipe(
            tap(() => {
              patchState(store, { hasChanged: true });
              store._messages.add({
                severity: 'success',
                summary: store._translate.instant('FILES.TOASTS.SUCCESS'),
                detail: store._translate.instant('DRAFTS.TOASTS.DISMISSED'),
              });
              store.drafts.reload();
            }),
            catchError((error: unknown) => {
              store._messages.add({
                severity: 'error',
                summary: store._translate.instant('ERRORS.SUMMARY'),
                detail: store._translate.instant(
                  draftConflictKey(error) ?? httpErrorMessageKey(error instanceof HttpErrorResponse ? error.status : 0),
                ),
              });
              store.drafts.reload();
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action, hasChanged: false });
      },
      /** Opens the review of a draft. A metric draft opens the metrics page with all pending metric drafts of the attempt. */
      requestReview(row: DraftRow): void {
        const attemptId = store.attemptId();
        if (row.kind === DraftKind.Machine) {
          reviewMachineDraft(store._actions, row.id, row.isCreate);
        } else if (row.kind === DraftKind.Status && row.machineId !== null && attemptId !== undefined) {
          reviewStatusDraft(store._actions, { machineId: row.machineId, attemptId, draftId: row.id });
        } else if (row.kind === DraftKind.Metric && row.machineId !== null && attemptId !== undefined) {
          void reviewMetricDrafts(store._actions, store._router, row.machineId, attemptId);
        }
      },
      /** Applies one draft without the review screen. */
      requestApply(row: DraftRow): void {
        const attemptId = store.attemptId();
        if (attemptId !== undefined) {
          store.applyDraft(attemptId, row.id);
        }
      },
      /** Applies the machine draft, then the metric drafts in one call, then the status drafts of one group. */
      requestApplyAll(groupKey: string): void {
        const attemptId = store.attemptId();
        const group = store.groups().find((candidate) => candidate.key === groupKey);
        if (attemptId !== undefined && group !== undefined) {
          store.applyGroup(attemptId, { machineDraftId: group.machineDraftId, machineId: group.machineId });
        }
      },
      /** Asks for a confirmation, dismisses the draft, and loads the list again. */
      requestDismiss(row: DraftRow): void {
        store._confirmation.confirm({
          header: store._translate.instant('DRAFTS.DISMISS_HEADER'),
          message: store._translate.instant('DRAFTS.DISMISS_MESSAGE', {
            name: row.label ?? store._translate.instant('DRAFTS.UNNAMED'),
          }),
          icon: 'pi pi-exclamation-triangle',
          acceptLabel: store._translate.instant('DRAFTS.DISMISS'),
          rejectLabel: store._translate.instant('ACTIONS.CANCEL'),
          acceptButtonProps: { severity: 'danger' },
          rejectButtonProps: { severity: 'secondary' },
          accept: () => {
            dismissDraft(row);
          },
        });
      },
      /** Closes the dialog. After a dismiss, the close tells the other lists of drafts to reload. */
      close(): void {
        if (store.hasChanged()) {
          store._actions.complete(DraftActionType.Changed);
        } else {
          store._actions.cancel();
        }
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        const attemptId = store.attemptId();
        store.setQuickDrafts(
          attemptId !== undefined && store.drafts.hasValue() ? [{ attemptId, drafts: store.drafts.value() }] : [],
        );
      });
      let seen = 0;
      effect(() => {
        const run = store.quickRun();
        if (run.count > seen) {
          seen = run.count;
          untracked(() => {
            if (run.hasApplied) {
              patchState(store, { hasChanged: true });
            }
            store.drafts.reload();
          });
        }
      });
    },
  }),
);
