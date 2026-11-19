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
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { payloadOf, toCreateBody, toUpdateBody } from './work-order-drawer.store.utils';

interface ActionState {
  action: AppAction | null;
  isHistoryActive: boolean;
  isSaving: boolean;
}

/**
 * The open action of the drawer: the work order and its history that it loads, what the user may do, the save in
 * progress, and the save that creates or updates the work order.
 */
export function withWorkOrderDrawerAction() {
  return signalStoreFeature(
    {
      props: type<{ isViewMode: Signal<boolean> }>(),
      methods: type<{
        _toastSuccess: (key: string) => void;
        _toastError: (key: string) => void;
        _startMode: (isUpdate: boolean) => void;
        view: () => void;
      }>(),
    },
    withState<ActionState>({ action: null, isHistoryActive: false, isSaving: false }),
    withComputed((store) => {
      const auth = inject(AuthStore);
      return {
        isUpdate: computed(() => payloadOf(store.action()).workOrderId !== undefined),
        workOrderId: computed(() => payloadOf(store.action()).workOrderId),
        canManage: computed(() => auth.hasPermission(Permission.WorkOrdersManage)),
        canWrite: computed(() => auth.hasPermission(Permission.WorkOrdersWrite)),
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
          params: () => (store.isHistoryActive() ? store.workOrderId() : undefined),
          stream: ({ params }) => api.history(params),
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
        patchState(store, { action, isHistoryActive: false });
        store._startMode(payloadOf(action).workOrderId !== undefined);
      },
      cancelEdit(): void {
        store.view();
      },
      /** Starts the loading of the history: the History tab calls it when it opens, so a closed tab sends no request. */
      activateHistory(): void {
        patchState(store, { isHistoryActive: true });
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
          store.workOrder.reload();
          store.history.reload();
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
                    store.workOrder.reload();
                    store.history.reload();
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
