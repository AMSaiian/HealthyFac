import { HttpStatusCode } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { withConfirm } from '@shared/confirm/with-confirm';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { RealtimeTopic } from '@shared/realtime/realtime';
import { withRealtimeReload } from '@shared/realtime/with-realtime-reload';
import { withGridState } from '@shared/grid/with-grid-state';
import { withGridRefresh } from '@shared/grid/with-grid-refresh';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { withToasts } from '@shared/toasts/with-toasts';

import {
  type ApplyWorkOrderDraftBody,
  DraftState,
  type WorkOrderDraft,
  type WorkOrderDraftColumn,
  type WorkOrderDraftFilter,
  type WorkOrderDraftSortColumn,
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
  /** The draft that a link named by its ID, which the grid page may not hold. */
  linkedDraft: WorkOrderDraft | null;
  isSaving: boolean;
}

export const WorkOrderDraftsGridStore = signalStore(
  withGridState<WorkOrderDraftFilter, WorkOrderDraftSortColumn, WorkOrderDraftColumn>(INITIAL_GRID),
  withState<ScopeState>({ editorDraftId: null, linkedDraft: null, isSaving: false }),
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
      if (draftId === null) {
        return null;
      }
      const linked = store.linkedDraft();
      const listed = store.drafts.hasValue() ? store.drafts.value().items.find((draft) => draft.id === draftId) : null;
      return listed ?? (linked?.id === draftId ? linked : null);
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

    const loadLinkedDraft = rxMethod<string>(
      pipe(
        exhaustMap((draftId) =>
          store._api.get(draftId, skipErrorToast()).pipe(
            tap((draft) => {
              if (draft.state !== DraftState.Pending) {
                store._toastError(NOT_PENDING_KEY);
                return;
              }
              patchState(store, { linkedDraft: draft, editorDraftId: draft.id });
            }),
            catchError((error: unknown) => {
              store._toastError(httpErrorKey(error, { [HttpStatusCode.UnprocessableEntity]: NOT_PENDING_KEY }));
              return EMPTY;
            }),
          ),
        ),
      ),
    );

    return {
      /** Opens the apply dialog of the draft that a link names (`?draftId=`). Without the right to apply, it does nothing. */
      openLinkedDraft(draftId: string): void {
        if (store.canApply()) {
          loadLinkedDraft(draftId);
        }
      },
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
  withRealtimeReload<{ drafts: { reload: () => boolean } }>([RealtimeTopic.WorkOrders], (store) => {
    store.drafts.reload();
  }),
  withGridRefresh<{ drafts: { reload: () => boolean } }>((store) => {
    store.drafts.reload();
  }),
  withStoreDevtools('work-order-drafts-grid'),
);
