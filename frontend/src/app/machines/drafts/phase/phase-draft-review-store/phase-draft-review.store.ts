import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, exhaustMap, finalize, pipe, tap } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ActionStore } from '@shared/actions/action.store';
import type { AppAction } from '@shared/actions/app-action';
import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { httpErrorKey, skipErrorToast } from '@shared/http/http-error-key';
import { CLOCK } from '@shared/time/clock';
import { withToasts } from '@shared/toasts/with-toasts';

import { draftScopeOf } from '@machines/drafts/draft';
import type { PhaseDraftReviewActionPayload } from '@machines/drafts/draft-action-payloads';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { draftConflictKey } from '@machines/drafts/draft-conflict';
import { DraftsApi } from '@machines/drafts/drafts-api';
import {
  findPhaseDraft,
  phaseReviewBlock,
  toDraftFormValue,
  toSourceNames,
  waitsForDrafts,
} from '@machines/drafts/phase/phase-draft-review-store/phase-draft-review.store.utils';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import type { PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import { toBody, toSourceOptions } from '@machines/phases/phase-form/phase-form.utils';
import { toSearchBody } from '@machines/statuses/list/status-definition-query';
import { INITIAL_GRID } from '@machines/statuses/list/status-definitions-grid.store.utils';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

/** The most statuses that the review names: the largest page that the API accepts. */
const STATUS_NAME_LIMIT = 100;

interface ReviewState {
  action: AppAction | null;
  isSaving: boolean;
  /** The values that the phase form reports. The form is the one place that edits them. */
  formValue: PhaseFormValue | null;
  isFormValid: boolean;
  /** The priority that the user typed, or `undefined` while the draft priority holds. */
  editedPriority: number | null | undefined;
}

/**
 * The review of one phase draft. It reads the drafts of the attempt and the metrics and statuses of the machine for
 * the names, and it applies the draft with its own values. A `422` shows its text and keeps the dialog open.
 */
export const PhaseDraftReviewStore = signalStore(
  withState<ReviewState>({
    action: null,
    isSaving: false,
    formValue: null,
    isFormValid: false,
    editedPriority: undefined,
  }),
  withToasts(),
  withProps(() => ({
    _draftsApi: inject(DraftsApi),
    _metricsApi: inject(MetricDefinitionsApi),
    _statusesApi: inject(StatusDefinitionsApi),
    _actions: inject(ActionStore),
    _clock: inject(CLOCK),
  })),
  withComputed((store) => ({
    payload: computed(() => store.action()?.payload as PhaseDraftReviewActionPayload | undefined),
  })),
  withProps((store) => ({
    drafts: rxResource({
      params: () => draftScopeOf(store.payload()),
      stream: ({ params }) => store._draftsApi.listForScope(params),
    }),
    metrics: rxResource({
      params: () => store.payload()?.machineId,
      stream: ({ params }) => store._metricsApi.getDefinitions(params, skipErrorToast()),
    }),
    statuses: rxResource({
      params: () => store.payload()?.machineId,
      stream: ({ params }) =>
        store._statusesApi.search(
          params,
          toSearchBody({ ...INITIAL_GRID, offset: 0, limit: STATUS_NAME_LIMIT }, store._clock.timeZoneId()),
          skipErrorToast(),
        ),
    }),
  })),
  withComputed((store) => {
    const draft = computed(() => {
      const payload = store.payload();
      return store.drafts.hasValue() && payload !== undefined
        ? findPhaseDraft(store.drafts.value(), payload.draftId)
        : null;
    });
    const priority = computed(() => {
      const edited = store.editedPriority();
      return edited === undefined ? (draft()?.priority ?? null) : edited;
    });
    return {
      draft,
      priority,
      /** A draft that names a pending metric or status draft stays read-only: Telemetry refuses its apply. */
      isEditable: computed(() => {
        const current = draft();
        return current !== null && !waitsForDrafts(current);
      }),
      initialValue: computed(() => {
        const current = draft();
        return current === null ? null : toDraftFormValue(current);
      }),
      sourceOptions: computed(() =>
        toSourceOptions(
          store.statuses.hasValue() ? store.statuses.value().items : [],
          store.metrics.hasValue() ? store.metrics.value().definitions : [],
        ),
      ),
      metricList: computed(() => (store.metrics.hasValue() ? store.metrics.value().definitions : [])),
      statusIds: computed(
        () => new Set((store.statuses.hasValue() ? store.statuses.value().items : []).map((status) => status.id)),
      ),
      isLoading: computed(
        () =>
          store.payload() === undefined ||
          store.drafts.isLoading() ||
          store.metrics.isLoading() ||
          store.statuses.isLoading(),
      ),
      names: computed(() =>
        toSourceNames(
          store.drafts.hasValue() ? store.drafts.value() : { groups: [] },
          store.metrics.hasValue() ? store.metrics.value().definitions : [],
          store.statuses.hasValue() ? store.statuses.value().items : [],
        ),
      ),
      /** The translation key of the reason that **Apply** is off, or `null`. */
      applyBlock: computed(() => {
        const current = draft();
        return current === null ? 'DRAFTS.PHASE.MISSING' : phaseReviewBlock(current, priority(), store.isFormValid());
      }),
    };
  }),
  withMethods((store) => {
    const apply = rxMethod<void>(
      pipe(
        tap(() => {
          patchState(store, { isSaving: true });
        }),
        exhaustMap(() => {
          const payload = store.payload();
          const draft = store.draft();
          const value = store.formValue();
          const priority = store.priority();
          const body =
            draft === null || value === null || priority === null ? null : toBody(value, priority, store.statusIds());
          if (payload === undefined || body === null) {
            patchState(store, { isSaving: false });
            return EMPTY;
          }
          return store._draftsApi.applyPhaseDraft(payload.machineId, payload.draftId, body, skipErrorToast()).pipe(
            tap(() => {
              store._toastSuccess('DRAFTS.PHASE.APPLIED');
              store._actions.complete(DraftActionType.Changed);
            }),
            catchError((error: unknown) => {
              store._toastError(draftConflictKey(error) ?? httpErrorKey(error));
              return EMPTY;
            }),
            finalize(() => {
              patchState(store, { isSaving: false });
            }),
          );
        }),
      ),
    );

    return {
      start(action: AppAction): void {
        patchState(store, { action });
      },
      setFormValue(formValue: PhaseFormValue): void {
        patchState(store, { formValue });
      },
      setFormValidity(isFormValid: boolean): void {
        patchState(store, { isFormValid });
      },
      setPriority(editedPriority: number | null): void {
        patchState(store, { editedPriority });
      },
      apply(): void {
        if (store.applyBlock() === null) {
          apply();
        }
      },
      cancel(): void {
        store._actions.cancel();
      },
    };
  }),
  withStoreDevtools('phase-draft-review'),
);
