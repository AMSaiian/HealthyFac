import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withConfirm } from '@shared/confirm/with-confirm';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { withGridState } from '@shared/grid/with-grid-state';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import type {
  ApplyWorkOrderDraftBody,
  WorkOrderDraft,
  WorkOrderDraftColumn,
  WorkOrderDraftFilter,
  WorkOrderDraftSortColumn,
} from '@work-orders/drafts/work-order-draft';
import { WorkOrderDraftsApi } from '@work-orders/drafts/work-order-drafts-api';
import { INITIAL_GRID, toSearchBody } from '@work-orders/drafts/work-order-drafts-grid.store.utils';

const NOT_PENDING_KEY = 'DRAFTS.CONFLICTS.DRAFT_NOT_PENDING';
const CHANGED_KEY = 'WORK_ORDER_DRAFTS.CONFLICTS.CHANGED';

/** One apply of the dialog. `rxMethod` returns no result, so the caller passes the draft with the body. */
interface ApplyRequest {
  readonly draftId: string;
  readonly body: ApplyWorkOrderDraftBody;
}

interface ScopeState {
  editorDraftId: string | null;
  isSaving: boolean;
}

export const WorkOrderDraftsGridStore = signalStore(
  withGridState<WorkOrderDraftFilter, WorkOrderDraftSortColumn, WorkOrderDraftColumn>(INITIAL_GRID),
  withState<ScopeState>({ editorDraftId: null, isSaving: false }),
  withProps(() => ({
    _auth: inject(AuthStore),
    _api: inject(WorkOrderDraftsApi),
  })),
  withToasts(),
  withConfirm(),
  withComputed((store) => ({
    canApply: computed(
      () => store._auth.hasPermission(Permission.DraftsWrite) && store._auth.hasPermission(Permission.WorkOrdersWrite),
    ),
    canDismiss: computed(() => store._auth.hasPermission(Permission.DraftsWrite)),
  })),
  withProps((store) => ({
    drafts: rxResource({
      params: () =>
        toSearchBody({ offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() }),
      stream: ({ params }) => store._api.search(params),
    }),
  })),
  withComputed((store) => ({
    editedDraft: computed((): WorkOrderDraft | null => {
      const draftId = store.editorDraftId();
      return draftId === null || !store.drafts.hasValue()
        ? null
        : (store.drafts.value().items.find((draft) => draft.id === draftId) ?? null);
    }),
  })),
  withMethods((store) => {
    const afterConflict = (error: unknown): Observable<never> => {
      store._toastError(
        httpErrorKey(error, {
          [HttpStatusCode.UnprocessableEntity]: NOT_PENDING_KEY,
          [HttpStatusCode.Conflict]: CHANGED_KEY,
        }),
      );
      patchState(store, { editorDraftId: null });
      store.drafts.reload();
      return EMPTY;
    };
    const applyDraft = rxMethod<ApplyRequest>(
      pipe(
        tap(() => patchState(store, { isSaving: true })),
        exhaustMap(({ draftId, body }) =>
          store._api.apply(draftId, body, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('WORK_ORDER_DRAFTS.TOASTS.APPLIED');
              patchState(store, { editorDraftId: null });
              store.drafts.reload();
            }),
            catchError(afterConflict),
            finalize(() => patchState(store, { isSaving: false })),
          ),
        ),
      ),
    );
    const dismissDraft = rxMethod<WorkOrderDraft>(
      pipe(
        exhaustMap((draft) =>
          store._api.dismiss(draft.id, { rowVersion: draft.rowVersion }, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('WORK_ORDER_DRAFTS.TOASTS.DISMISSED');
              store.drafts.reload();
            }),
            catchError(afterConflict),
          ),
        ),
      ),
    );

    return {
      requestApply(draftId: string): void {
        patchState(store, { editorDraftId: draftId });
      },
      closeEditor(): void {
        patchState(store, { editorDraftId: null });
      },
      apply(body: ApplyWorkOrderDraftBody): void {
        const draftId = store.editorDraftId();
        if (draftId !== null) {
          applyDraft({ draftId, body });
        }
      },
      requestDismiss(draft: WorkOrderDraft): void {
        store._confirmDestructive({
          headerKey: 'WORK_ORDER_DRAFTS.DISMISS_HEADER',
          messageKey: 'WORK_ORDER_DRAFTS.DISMISS_MESSAGE',
          params: { title: draft.title },
          acceptKey: 'WORK_ORDER_DRAFTS.DISMISS',
          onAccept: () => {
            dismissDraft(draft);
          },
        });
      },
    };
  }),
  withStoreDevtools('work-order-drafts-grid'),
);
