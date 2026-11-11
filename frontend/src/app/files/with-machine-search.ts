import { computed, effect, inject, type ResourceRef, type Signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  type EmptyFeatureResult,
  patchState,
  type SignalStoreFeature,
  signalStoreFeature,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { debounceTime, distinctUntilChanged, pipe, tap } from 'rxjs';

import type { LookupOption } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';

import type { MachineOption } from '@files/file';

/** The pause after the last typed letter before the search goes to the server. */
export const MACHINE_SEARCH_DEBOUNCE_MILLISECONDS = 300;

interface MachineSearchState {
  isMachineSearchEnabled: boolean;
  machineQuery: string;
  selectedMachineIds: readonly string[];
  /** The name of every machine that a search returned, so a selected machine keeps its name in later results. */
  knownMachines: Readonly<Record<string, string>>;
}

/**
 * A server search of the machines for a select: the typed text goes to the server after a pause, and a machine that
 * the user selected stays in the options when a later result does not hold it. The files grid filter and the tie
 * dialog share it. The search runs only while it is enabled.
 */
export function withMachineSearch(): SignalStoreFeature<
  EmptyFeatureResult,
  {
    state: MachineSearchState;
    props: {
      _lookups: LookupsApi;
      machines: ResourceRef<LookupOption[] | undefined>;
      machineOptions: Signal<MachineOption[]>;
    };
    methods: {
      enableMachineSearch: (isEnabled: boolean) => void;
      setSelectedMachines: (ids: readonly string[]) => void;
      searchMachines: (text: string) => void;
    };
  }
> {
  return signalStoreFeature(
    withState<MachineSearchState>({
      isMachineSearchEnabled: false,
      machineQuery: '',
      selectedMachineIds: [],
      knownMachines: {},
    }),
    withProps(() => ({ _lookups: inject(LookupsApi) })),
    withProps((store) => ({
      machines: rxResource({
        params: () => (store.isMachineSearchEnabled() ? store.machineQuery() : undefined),
        stream: ({ params }) => store._lookups.machines(params),
      }),
    })),
    withComputed((store) => ({
      /** The search result plus the selected machines that the result does not hold, by name. */
      machineOptions: computed((): MachineOption[] => {
        const found: MachineOption[] = store.machines.hasValue()
          ? store.machines.value().map(({ id, displayName }) => ({ id, name: displayName }))
          : [];
        const foundIds = new Set(found.map((machine) => machine.id));
        const known = store.knownMachines();
        const kept = store
          .selectedMachineIds()
          .filter((id) => !foundIds.has(id) && known[id] !== undefined)
          .map((id) => ({ id, name: known[id] }));
        return [...found, ...kept].sort((left, right) => left.name.localeCompare(right.name));
      }),
    })),
    withMethods((store) => ({
      enableMachineSearch(isEnabled: boolean): void {
        patchState(store, { isMachineSearchEnabled: isEnabled });
      },
      setSelectedMachines(ids: readonly string[]): void {
        patchState(store, { selectedMachineIds: ids });
      },
      /** Searches the machines by the typed text, after a pause and only when the text changed. */
      searchMachines: rxMethod<string>(
        pipe(
          debounceTime(MACHINE_SEARCH_DEBOUNCE_MILLISECONDS),
          distinctUntilChanged(),
          tap((machineQuery) => patchState(store, { machineQuery })),
        ),
      ),
    })),
    withHooks({
      onInit(store) {
        effect(() => {
          if (store.machines.hasValue()) {
            const items = store.machines.value();
            untracked(() =>
              patchState(store, (state) => ({
                knownMachines: {
                  ...state.knownMachines,
                  ...Object.fromEntries(items.map((machine) => [machine.id, machine.displayName])),
                },
              })),
            );
          }
        });
      },
    }),
  );
}
