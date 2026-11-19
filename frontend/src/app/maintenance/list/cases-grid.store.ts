import { computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { signalStore, withComputed, withMethods, withProps } from '@ngrx/signals';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';
import { withGridState } from '@shared/grid/with-grid-state';
import { createLookup } from '@shared/lookups/create-lookup';
import { labelsById } from '@shared/lookups/lookup-options';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import type { CaseColumn, CaseFilter, CaseSortColumn } from '@maintenance/case';
import { CasesApi } from '@maintenance/cases-api';
import { INITIAL_GRID, toSearchBody } from '@maintenance/list/cases-grid.store.utils';

const CASES_PATH = '/maintenance/cases';

export const CasesGridStore = signalStore(
  withGridState<CaseFilter, CaseSortColumn, CaseColumn>(INITIAL_GRID),
  withProps(() => ({
    _auth: inject(AuthStore),
    _api: inject(CasesApi),
    _lookups: inject(LookupsApi),
    _router: inject(Router),
  })),
  withComputed((store) => ({
    canWrite: computed(() => store._auth.hasPermission(Permission.CasesWrite)),
  })),
  withProps((store) => ({
    cases: rxResource({
      params: () =>
        toSearchBody({ offset: store.offset(), limit: store.limit(), sort: store.sort(), filter: store.filter() }),
      stream: ({ params }) => store._api.search(params),
    }),
    machineLookup: createLookup(
      (search) => store._lookups.machines(search),
      () => store.filter().machineIds,
    ),
  })),
  withProps((store) => ({
    /** The names of the machines of the loaded page: one call per page, none for an empty page. */
    machines: rxResource({
      params: () => {
        const ids = store.cases.hasValue() ? [...new Set(store.cases.value().items.map((item) => item.machineId))] : [];
        return ids.length === 0 ? undefined : ids;
      },
      stream: ({ params }) => store._lookups.machinesByIds(params),
      defaultValue: [],
    }),
  })),
  withComputed((store) => ({
    machineLabels: computed(() => labelsById(store.machines.hasValue() ? store.machines.value() : [])),
  })),
  withMethods((store) => ({
    openCase(id: string): void {
      void store._router.navigate([CASES_PATH, id]);
    },
  })),
  withStoreDevtools('cases-grid'),
);
