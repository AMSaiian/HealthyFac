import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, Subject, throwError } from 'rxjs';

import { SortDirection } from '@shared/grid/requests/grid-request';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';
import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
import type { Machine } from '@machines/machine';
import {
  type MachineErrorCode,
  MachineErrorCodeColumn,
  MachineErrorCodeSortColumn,
} from '@machines/error-codes/machine-error-code';
import { MachineErrorCodesApi } from '@machines/error-codes/machine-error-codes-api';
import { MachinesApi } from '@machines/machines-api';

import { MachineErrorCodesGridStore } from './machine-error-codes-grid.store';

const MACHINE_ID = 'encrypted-machine-id';
const OTHER_MACHINE_ID = 'other-encrypted-machine-id';
const EMPTY_PAGE = { items: [], totalCount: 0 };

const ERROR_CODE: MachineErrorCode = {
  id: 'encrypted-id',
  code: 'E-101',
  description: null,
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
};

interface Setup {
  store: InstanceType<typeof MachineErrorCodesGridStore>;
  search: ReturnType<typeof vi.fn>;
  get: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  messages: { add: ReturnType<typeof vi.fn> };
}

describe('MachineErrorCodesGridStore', () => {
  async function setup(
    options: { hasPermission?: boolean; isConfirmed?: boolean; isMachineSet?: boolean } = {},
  ): Promise<Setup> {
    const messages = { add: vi.fn() };
    const search = vi.fn(() => of(EMPTY_PAGE));
    const remove = vi.fn(() => of(undefined));
    const get = vi.fn(() => of({ name: 'Lathe 01' }));
    TestBed.configureTestingModule({
      providers: [
        MachineErrorCodesGridStore,
        { provide: MachineErrorCodesApi, useValue: { search, delete: remove } },
        { provide: MachinesApi, useValue: { get } },
        { provide: MessageService, useValue: messages },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: vi.fn((confirmation: { accept?: () => void; reject?: () => void }) =>
              options.isConfirmed === true ? confirmation.accept?.() : confirmation.reject?.(),
            ),
          },
        },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        { provide: AuthStore, useValue: { hasPermission: () => options.hasPermission ?? true } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(MachineErrorCodesGridStore);
    if (options.isMachineSet !== false) {
      store.openMachine(MACHINE_ID).subscribe();
    }
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return { store, search, get, remove, messages };
  }

  it('requestCreate_Always_OpensDialogWithoutErrorCodeId', async () => {
    const { store } = await setup();

    store.requestCreate();

    expect(store.editor()).toEqual({ errorCodeId: null });
  });

  it('requestUpdate_WithId_OpensDialogForThatErrorCode', async () => {
    const { store } = await setup();

    store.requestUpdate(ERROR_CODE.id);

    expect(store.editor()).toEqual({ errorCodeId: ERROR_CODE.id });
  });

  it('onSaved_WithOpenDialog_ClosesItAndReloadsGrid', async () => {
    const { store, search } = await setup();
    store.requestCreate();
    const callsBefore = search.mock.calls.length;

    store.onSaved();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.editor()).toBeNull();
    expect(search.mock.calls.length - callsBefore).toBe(1);
  });

  it('closeEditor_WithOpenDialog_ClosesItWithoutReload', async () => {
    const { store, search } = await setup();
    store.requestCreate();
    const callsBefore = search.mock.calls.length;

    store.closeEditor();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.editor()).toBeNull();
    expect(search.mock.calls.length).toBe(callsBefore);
  });

  it('resources_BeforeMachineIdIsSet_SendNoRequest', async () => {
    const { search, get } = await setup({ isMachineSet: false });

    expect(search).not.toHaveBeenCalled();
    expect(get).not.toHaveBeenCalled();
  });

  it('openMachine_WithId_SendsSearchAndGetWithThatId', async () => {
    const { store, search, get } = await setup({ isMachineSet: false });

    store.openMachine(MACHINE_ID).subscribe();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls[0][0]).toBe(MACHINE_ID);
    expect(get).toHaveBeenCalledWith(MACHINE_ID, expect.anything());
  });

  async function dirtyThenOpen(
    store: InstanceType<typeof MachineErrorCodesGridStore>,
    firstMachineId: string,
    secondMachineId: string,
  ): Promise<void> {
    store.openMachine(firstMachineId).subscribe();
    store.setFilter({ code: 'e-1' });
    store.setPage(50, 10);
    store.setSort({ column: MachineErrorCodeSortColumn.CreatedAt, direction: SortDirection.Descending });
    store.setColumns([MachineErrorCodeColumn.Code]);
    store.requestCreate();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.openMachine(secondMachineId).subscribe();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  function expectInitialSearch(search: ReturnType<typeof vi.fn>, machineId: string): void {
    const [lastMachineId, lastBody] = search.mock.calls[search.mock.calls.length - 1];

    expect(lastMachineId).toBe(machineId);
    expect(lastBody).toEqual({
      offset: 0,
      limit: 25,
      sort: { column: 'code', direction: 'asc' },
      filter: {},
    });
  }

  it('openMachine_AfterOtherMachineWasChanged_StartsFromInitialGridState', async () => {
    const { store, search } = await setup({ isMachineSet: false });

    await dirtyThenOpen(store, MACHINE_ID, OTHER_MACHINE_ID);

    expectInitialSearch(search, OTHER_MACHINE_ID);
    expect(store.visibleColumns()).toEqual(Object.values(MachineErrorCodeColumn));
    expect(store.editor()).toBeNull();
  });

  it('openMachine_ForSameMachineAgain_StartsFromInitialGridState', async () => {
    const { store, search } = await setup({ isMachineSet: false });

    await dirtyThenOpen(store, MACHINE_ID, MACHINE_ID);

    expectInitialSearch(search, MACHINE_ID);
    expect(store.visibleColumns()).toEqual(Object.values(MachineErrorCodeColumn));
    expect(store.editor()).toBeNull();
  });

  it('openMachine_WhileLoadIsPending_KeepsCurrentMachineAndSendsNoSearch', async () => {
    const { store, search, get } = await setup();
    store.setFilter({ code: 'e-1' });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    get.mockReturnValueOnce(new Subject<Machine>());
    const callsBefore = search.mock.calls.length;

    store.openMachine(OTHER_MACHINE_ID).subscribe();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length).toBe(callsBefore);
    expect(store.machineId()).toBe(MACHINE_ID);
    expect(store.filter().code).toBe('e-1');
  });

  it('openMachine_WhenLoadFails_KeepsCurrentMachineAndSendsNoSearch', async () => {
    const { store, search, get } = await setup();
    get.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));
    const callsBefore = search.mock.calls.length;

    store.openMachine(OTHER_MACHINE_ID).subscribe({ error: () => undefined });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length).toBe(callsBefore);
    expect(store.machineId()).toBe(MACHINE_ID);
    expect(store.machine()).toEqual({ name: 'Lathe 01' });
  });

  it('openMachine_WithId_StoresLoadedMachineAndLoadsWithoutErrorToast', async () => {
    const { store, get } = await setup({ isMachineSet: false });

    store.openMachine(MACHINE_ID).subscribe();

    const [, options] = get.mock.calls[0];
    expect(store.machine()).toEqual({ name: 'Lathe 01' });
    expect(options.context.get(SKIP_ERROR_TOAST)).toBe(true);
  });

  it('canDelete_WithoutPermission_IsFalse', async () => {
    const { store } = await setup({ hasPermission: false });

    expect(store.canDelete()).toBe(false);
    expect(store.canWrite()).toBe(false);
  });

  it('requestDelete_WhenRejected_SendsNoRequest', async () => {
    const { store, remove } = await setup({ isConfirmed: false });

    store.requestDelete(ERROR_CODE);
    TestBed.tick();

    expect(remove).not.toHaveBeenCalled();
  });

  it('requestDelete_WhenConfirmed_DeletesShowsToastAndReloads', async () => {
    const { store, remove, search, messages } = await setup({ isConfirmed: true });
    const callsBefore = search.mock.calls.length;

    store.requestDelete(ERROR_CODE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(remove).toHaveBeenCalledWith(MACHINE_ID, ERROR_CODE.id);
    expect(search.mock.calls.length - callsBefore).toBe(1);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });
});
