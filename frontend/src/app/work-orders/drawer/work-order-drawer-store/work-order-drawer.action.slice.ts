import { HttpStatusCode } from '@angular/common/http';
import { computed, effect, inject, type Signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStoreFeature,
  type,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, type Observable, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { httpErrorKey, isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';
import { descriptionLinks } from '@shared/rich-text/file-image/description-links';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import { type WorkOrder, WorkOrderStatus } from '@work-orders/work-order';
import { type SavedDetail, WorkOrderActionType } from '@work-orders/work-order-actions';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';
import { HISTORY_PAGE_SIZE } from '@work-orders/work-order-limits';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { payloadOf, toCreateBody, toUpdateBody } from './work-order-drawer.store.utils';

interface ActionState {
  action: AppAction | null;
  isHistoryActive: boolean;
  /** The `after` revisions of the pages that the user moved through. Empty means the first page. */
  historyAfters: readonly number[];
  isSaving: boolean;
  /** Whether the form of the open work order holds changes that the user did not save. The host sets it. */
  hasUnsavedEdits: boolean;
  /** Whether the work order changed on the server while the user edited it. The form keeps its changes. */
  isChangedRemotely: boolean;
}

/**
 * The open action of the drawer: the work order and its history that it loads, what the user may do, the save in
 * progress, and the save that creates or updates the work order.
 */
export function withWorkOrderDrawerAction() {
  return signalStoreFeature(
    {
      props: type<{ isViewMode: Signal<boolean>; isEditMode: Signal<boolean> }>(),
      methods: type<{
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
        _startMode: (isUpdate: boolean) => void;
        view: () => void;
      }>(),
    },
    withState<ActionState>({
      action: null,
      isHistoryActive: false,
      historyAfters: [],
      isSaving: false,
      hasUnsavedEdits: false,
      isChangedRemotely: false,
    }),
    withComputed((store) => {
      const auth = inject(AuthStore);
      return {
        isUpdate: computed(() => payloadOf(store.action()).workOrderId !== undefined),
        workOrderId: computed(() => payloadOf(store.action()).workOrderId),
        canManage: computed(() => auth.hasPermission(Permission.WorkOrdersManage)),
        canWrite: computed(() => auth.hasPermission(Permission.WorkOrdersWrite)),
        historyPageIndex: computed(() => store.historyAfters().length),
        historyAfter: computed(() => store.historyAfters().at(-1)),
      };
    }),
    withProps((store) => {
      const api = inject(WorkOrdersApi);
      return {
        workOrder: rxResource({
          params: () => store.workOrderId(),
          stream: ({ params }) => api.get(params),
        }),
        history: rxResource({
          params: () =>
            store.isHistoryActive() && store.workOrderId() !== undefined
              ? { id: store.workOrderId() ?? '', after: store.historyAfter() }
              : undefined,
          stream: ({ params }) => api.history(params.id, HISTORY_PAGE_SIZE, params.after),
        }),
      };
    }),
    withProps((store) => ({
      descriptionLinks: descriptionLinks(
        () => (store.workOrder.hasValue() ? store.workOrder.value().description : null),
        inject(RichTextImageLinks),
      ).links,
    })),
    withComputed((store) => ({
      /** Tells whether the pencil shows: the loaded item in view mode, for a user who may change it. */
      isEditShown: computed(
        () => store.isViewMode() && store.isUpdate() && store.workOrder.hasValue() && store.canWrite(),
      ),
    })),
    withMethods((store) => ({
      start(action: AppAction): void {
        patchState(store, {
          action,
          isHistoryActive: false,
          historyAfters: [],
          hasUnsavedEdits: false,
          isChangedRemotely: false,
        });
        store._startMode(payloadOf(action).workOrderId !== undefined);
      },
      setHasUnsavedEdits(hasUnsavedEdits: boolean): void {
        patchState(store, { hasUnsavedEdits });
      },
      /** Starts the loading of the history: the History tab calls it when it opens, so a closed tab sends no request. */
      activateHistory(): void {
        patchState(store, { isHistoryActive: true });
      },
      /** Shows the page after the shown one. `nextAfter` is the `nextAfter` of the shown page. */
      nextHistoryPage(nextAfter: number): void {
        patchState(store, (state) => ({ historyAfters: [...state.historyAfters, nextAfter] }));
      },
      previousHistoryPage(): void {
        patchState(store, (state) => ({ historyAfters: state.historyAfters.slice(0, -1) }));
      },
      /** Loads the first page again, also when the user stands on a later page. */
      _reloadHistory(): void {
        if (store.historyAfters().length === 0) {
          store.history.reload();
          return;
        }
        patchState(store, { historyAfters: [] });
      },
      _setSaving(isSaving: boolean): void {
        patchState(store, { isSaving });
      },
    })),
    withMethods((store) => {
      const api = inject(WorkOrdersApi);
      const actions = inject(ActionStore);

      const request$ = (value: WorkOrderFormValue): Observable<unknown> => {
        const options = skipErrorToast();
        if (!store.isUpdate()) {
          return api.create(toCreateBody(value), options);
        }
        const current: WorkOrder | undefined = store.workOrder.hasValue() ? store.workOrder.value() : undefined;
        return current === undefined
          ? EMPTY
          : api.update(store.workOrderId() ?? '', toUpdateBody(value, current, store.canManage()), options);
      };

      const handleFailure = (error: unknown): void => {
        if (isHttpStatus(error, HttpStatusCode.Conflict)) {
          store._toastError('WORK_ORDERS.TOASTS.CONFLICT');
          patchState(store, { isChangedRemotely: false });
          store.workOrder.reload();
          store._reloadHistory();
          return;
        }
        store._toastError(httpErrorKey(error, { [HttpStatusCode.UnprocessableEntity]: 'WORK_ORDERS.TOASTS.RULE' }));
      };

      return {
        save: rxMethod<WorkOrderFormValue>(
          pipe(
            tap(() => store._setSaving(true)),
            exhaustMap((value) =>
              request$(value).pipe(
                tap(() => {
                  if (store.isUpdate()) {
                    store._toastSuccess('WORK_ORDERS.TOASTS.UPDATED');
                    actions.notify(WorkOrderActionType.Saved);
                    patchState(store, { hasUnsavedEdits: false, isChangedRemotely: false });
                    store.workOrder.reload();
                    store._reloadHistory();
                    store.view();
                  } else {
                    store._toastSuccess('WORK_ORDERS.TOASTS.CREATED');
                    actions.complete(WorkOrderActionType.Saved, {
                      status: WorkOrderStatus.Backlog,
                    } satisfies SavedDetail);
                  }
                }),
                catchError((error: unknown) => {
                  handleFailure(error);
                  return EMPTY;
                }),
                finalize(() => store._setSaving(false)),
              ),
            ),
          ),
        ),
        cancel(): void {
          actions.cancel();
        },
        cancelEdit(): void {
          const wasChangedRemotely = store.isChangedRemotely();
          patchState(store, { hasUnsavedEdits: false, isChangedRemotely: false });
          store.view();
          if (wasChangedRemotely) {
            store.workOrder.reload();
            store._reloadHistory();
          }
        },
        /**
         * Reacts to a hint of the open work order: a view reloads the work order and its history (the first page); an
         * edit with unsaved changes keeps the form and shows a message instead.
         */
        reloadFromHint(): void {
          if (store.isEditMode() && store.hasUnsavedEdits()) {
            patchState(store, { isChangedRemotely: true });
            return;
          }
          store.workOrder.reload();
          store._reloadHistory();
        },
      };
    }),
    withHooks({
      onInit(store) {
        const actions = inject(ActionStore);
        effect(() => {
          if (store.workOrder.status() === 'error') {
            actions.cancel();
          }
        });
      },
    }),
  );
}
