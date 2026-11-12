import { linkedSignal, type Signal } from '@angular/core';
import { patchState, signalStoreFeature, type, withLinkedState, withMethods, withState } from '@ngrx/signals';

import type { MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import type { MetricEdit } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

interface FormState {
  formValue: MetricFormValue | null;
  isFormValid: boolean;
}

/** The open form of a metric: the item it edits (or a new one), its value, and whether the value is valid. */
export function withMetricsPageForm() {
  return signalStoreFeature(
    { props: type<{ definitionsValue: Signal<unknown> }>() },
    withState<FormState>({ formValue: null, isFormValid: false }),
    withLinkedState((store) => ({
      edit: linkedSignal<unknown, MetricEdit | null>({ source: store.definitionsValue, computation: () => null }),
    })),
    withMethods((store) => ({
      _openForm(edit: MetricEdit): void {
        patchState(store, { edit, formValue: null, isFormValid: false });
      },
      _closeForm(): void {
        patchState(store, { edit: null, formValue: null, isFormValid: false });
      },
      setFormValue(formValue: MetricFormValue | null): void {
        patchState(store, { formValue });
      },
      setFormValidity(isFormValid: boolean): void {
        patchState(store, { isFormValid });
      },
    })),
  );
}
