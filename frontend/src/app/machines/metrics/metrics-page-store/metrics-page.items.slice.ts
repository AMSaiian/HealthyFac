import { HttpStatusCode } from '@angular/common/http';
import { computed, inject, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStoreFeature,
  type,
  withComputed,
  withLinkedState,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';

import { isHttpStatus, skipErrorToast } from '@shared/http/http-error-key';

import type { MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import {
  firstItemId,
  isNewItem,
  itemsFromDefinitions,
  type MetricItem,
  newLocalId,
  type StagedDrafts,
} from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

interface ItemsState {
  newCount: number;
  isExampleOpen: boolean;
}

/**
 * The list of the metrics: the saved items, the new items, the selection, the server errors per item, and the example
 * dialog.
 */
export function withMetricsPageItems() {
  return signalStoreFeature(
    { state: type<{ machineId: string | null }>() },
    withState<ItemsState>({ newCount: 0, isExampleOpen: false }),
    withProps(() => ({
      _definitionsApi: inject(MetricDefinitionsApi),
      _unitsApi: inject(MetricUnitsApi),
    })),
    withProps((store) => ({
      definitions: rxResource({
        params: () => store.machineId() ?? undefined,
        stream: ({ params }) => store._definitionsApi.getDefinitions(params, skipErrorToast()),
      }),
      units: rxResource({
        params: () => store.machineId() ?? undefined,
        stream: () => store._unitsApi.getUnits(),
      }),
    })),
    withComputed((store) => ({
      definitionsValue: computed(() => (store.definitions.hasValue() ? store.definitions.value() : undefined)),
      revision: computed(() => (store.definitions.hasValue() ? store.definitions.value().revision : 0)),
      isNotRegistered: computed(() => isHttpStatus(store.definitions.error(), HttpStatusCode.NotFound)),
    })),
    withLinkedState((store) => ({
      items: (): readonly MetricItem[] =>
        store.definitions.hasValue() ? itemsFromDefinitions(store.definitions.value().definitions) : [],
      selectedId: (): string | null =>
        store.definitions.hasValue() ? firstItemId(itemsFromDefinitions(store.definitions.value().definitions)) : null,
      itemErrors: linkedSignal<unknown, Readonly<Record<string, readonly string[]>>>({
        source: store.definitionsValue,
        computation: () => ({}),
      }),
    })),
    withMethods((store) => {
      const replaceItem = (localId: string, change: (item: MetricItem) => MetricItem): void => {
        patchState(store, { items: store.items().map((item) => (item.localId === localId ? change(item) : item)) });
      };

      return {
        reload(): void {
          store.definitions.reload();
        },
        _resetNewCount(): void {
          patchState(store, { newCount: 0 });
        },
        openExample(): void {
          patchState(store, { isExampleOpen: true });
        },
        closeExample(): void {
          patchState(store, { isExampleOpen: false });
        },
        _setSelectedId(selectedId: string | null): void {
          patchState(store, { selectedId });
        },
        _setItemErrors(itemErrors: Readonly<Record<string, readonly string[]>>): void {
          patchState(store, { itemErrors });
        },
        _clearItemError(localId: string): void {
          patchState(store, { itemErrors: { ...store.itemErrors(), [localId]: [] } });
        },
        _setStaged(staged: Pick<StagedDrafts, 'items' | 'newCount'>): void {
          patchState(store, { items: staged.items, newCount: staged.newCount });
        },
        _addNewItem(value: MetricFormValue): void {
          const localId = newLocalId(store.newCount());
          const item: MetricItem = {
            localId,
            savedId: null,
            saved: null,
            value,
            isDeleted: false,
            draftId: null,
            sourceLocation: null,
          };
          patchState(store, { items: [...store.items(), item], newCount: store.newCount() + 1, selectedId: localId });
        },
        _replaceValue(localId: string, value: MetricFormValue): void {
          replaceItem(localId, (item) => ({ ...item, value }));
        },
        remove(localId: string): void {
          const item = store.items().find((candidate) => candidate.localId === localId);
          if (item === undefined) {
            return;
          }
          if (isNewItem(item)) {
            const rest = store.items().filter((candidate) => candidate.localId !== localId);
            patchState(store, {
              items: rest,
              selectedId: store.selectedId() === localId ? firstItemId(rest) : store.selectedId(),
            });
          } else {
            replaceItem(localId, (current) => ({ ...current, isDeleted: true }));
          }
        },
        restore(localId: string): void {
          replaceItem(localId, (item) => ({ ...item, isDeleted: false }));
        },
      };
    }),
  );
}
