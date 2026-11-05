import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
import { type Machine, MachineState } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { MachinesGridStore } from './machines-grid.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };

const MACHINE: Machine = {
  id: 'encrypted-id',
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  departmentId: 'department-1',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
};

interface Setup {
  store: InstanceType<typeof MachinesGridStore>;
  actions: InstanceType<typeof ActionStore>;
  search: ReturnType<typeof vi.fn>;
  remove: ReturnType<typeof vi.fn>;
  messages: { add: ReturnType<typeof vi.fn> };
}

describe('MachinesGridStore', () => {
  async function setup(options: { hasPermission?: boolean; isConfirmed?: boolean } = {}): Promise<Setup> {
    const messages = { add: vi.fn() };
    const search = vi.fn(() => of(EMPTY_PAGE));
    const remove = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({
      providers: [
        MachinesGridStore,
        { provide: MachinesApi, useValue: { search, delete: remove } },
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
    const store = TestBed.inject(MachinesGridStore);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return { store, actions: TestBed.inject(ActionStore), search, remove, messages };
  }

  afterEach(() => {
    vi.useRealTimers();
  });

  it('lastResult_WithSavedType_ReloadsMachines', async () => {
    const { actions, search } = await setup();
    const callsBefore = search.mock.calls.length;

    actions.complete(MachineActionType.Saved);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length - callsBefore).toBe(1);
  });

  it('lastResult_WithOtherType_DoesNotReload', async () => {
    const { actions, search } = await setup();
    const callsBefore = search.mock.calls.length;

    actions.complete('things.saved');
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(search.mock.calls.length).toBe(callsBefore);
  });

  it('canDelete_WithoutPermission_IsFalse', async () => {
    const { store } = await setup({ hasPermission: false });

    expect(store.canDelete()).toBe(false);
    expect(store.canWrite()).toBe(false);
  });

  it('requestDelete_WhenRejected_SendsNoRequest', async () => {
    const { store, remove } = await setup({ isConfirmed: false });

    store.requestDelete(MACHINE);
    TestBed.tick();

    expect(remove).not.toHaveBeenCalled();
  });

  it('requestDelete_WhenConfirmed_DeletesShowsToastAndReloads', async () => {
    const { store, remove, search, messages } = await setup({ isConfirmed: true });
    const callsBefore = search.mock.calls.length;

    store.requestDelete(MACHINE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(remove).toHaveBeenCalledWith(MACHINE.id);
    expect(search.mock.calls.length - callsBefore).toBe(1);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('requestUpdate_Always_DispatchesUpdateAction', async () => {
    const { store, actions } = await setup();
    const dispatch = vi.spyOn(actions, 'dispatch').mockResolvedValue();

    store.requestUpdate(MACHINE.id);

    expect(dispatch).toHaveBeenCalledWith({
      type: MachineActionType.Update,
      payload: { machineId: MACHINE.id },
    });
  });

  it('openErrorCodes_WithMachineId_NavigatesToErrorCodeRoute', async () => {
    const { store } = await setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    store.openErrorCodes(MACHINE.id);

    expect(navigate).toHaveBeenCalledWith(['/machines', MACHINE.id, 'error-codes']);
  });

  it('canReadErrorCodes_WithoutPermission_IsFalse', async () => {
    const { store } = await setup({ hasPermission: false });

    expect(store.canReadErrorCodes()).toBe(false);
  });
});
