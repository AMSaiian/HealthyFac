import { ApplicationRef } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';

import { MachinesApi } from '@machines/machines-api';

import { MACHINE_SEARCH_DEBOUNCE_MILLISECONDS, withMachineSearch } from './with-machine-search';

const SEARCH_URL = '/api/core/v1/machines/search';

const DEBOUNCE_MARGIN = 100;

const Store = signalStore(withMachineSearch());

describe('withMachineSearch', () => {
  function setup(): { store: InstanceType<typeof Store>; http: HttpTestingController } {
    TestBed.configureTestingModule({
      providers: [Store, MachinesApi, provideHttpClient(), provideHttpClientTesting()],
    });
    const store = TestBed.inject(Store);
    store.enableMachineSearch(true);
    return { store, http: TestBed.inject(HttpTestingController) };
  }

  async function flushSearch(
    http: HttpTestingController,
    items: readonly { id: string; name: string }[],
  ): Promise<unknown> {
    TestBed.tick();
    const request = http.expectOne(SEARCH_URL);
    request.flush({ items, totalCount: items.length });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    return request.request.body;
  }

  /** Waits longer than the pause of the search, so the debounced call has gone out. */
  async function waitForDebounce(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, MACHINE_SEARCH_DEBOUNCE_MILLISECONDS + DEBOUNCE_MARGIN));
  }

  it('searchMachines_WithTypedText_SendsOneDebouncedSearchWithTheNameFilter', async () => {
    const { store, http } = setup();
    await flushSearch(http, []);

    store.searchMachines('pu');
    store.searchMachines('pum');
    TestBed.tick();
    http.expectNone(SEARCH_URL);
    await waitForDebounce();
    const body = await flushSearch(http, [{ id: 'm1', name: 'Pump' }]);

    expect(body).toEqual({
      offset: 0,
      limit: 100,
      sort: { column: 'name', direction: 'asc' },
      filter: { name: { value: 'pum' } },
    });
    expect(store.machineOptions()).toEqual([{ id: 'm1', name: 'Pump' }]);
  });

  it('searchMachines_WithEmptyText_SendsNoNameFilter', async () => {
    const { store, http } = setup();
    await flushSearch(http, []);
    store.searchMachines('pump');
    await waitForDebounce();
    await flushSearch(http, []);

    store.searchMachines('');
    await waitForDebounce();
    const body = await flushSearch(http, []);

    expect((body as { filter: object }).filter).toEqual({});
  });

  it('machineOptions_WithSelectedMachineMissingFromTheResult_KeepsItWithItsName', async () => {
    const { store, http } = setup();
    await flushSearch(http, [{ id: 'm1', name: 'Pump' }]);
    store.setSelectedMachines(['m1']);

    store.searchMachines('lathe');
    await waitForDebounce();
    await flushSearch(http, [{ id: 'm2', name: 'Lathe' }]);

    expect(store.machineOptions()).toEqual([
      { id: 'm2', name: 'Lathe' },
      { id: 'm1', name: 'Pump' },
    ]);
  });

  it('enableMachineSearch_WhenOff_SendsNoSearch', () => {
    TestBed.configureTestingModule({
      providers: [Store, MachinesApi, provideHttpClient(), provideHttpClientTesting()],
    });
    TestBed.inject(Store);
    TestBed.tick();

    TestBed.inject(HttpTestingController).expectNone(SEARCH_URL);
  });
});
