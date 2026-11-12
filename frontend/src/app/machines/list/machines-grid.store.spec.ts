import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
import { type Machine, MachineState } from '@machines/machine';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { MachinesGridStore } from './machines-grid.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };

const MACHINE: Machine = {
  id: 'encrypted-id',
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  gatewayId: 'abcdefghijklmnop',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
  pendingDraftCount: 0,
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

  it.each(
    [
      { name: 'WithSavedType', resultType: MachineActionType.Saved },
      { name: 'WithDraftsChanged', resultType: DraftActionType.Changed },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('lastResult_%s_ReloadsMachines', async (_name, { resultType }) => {
    const { actions, search } = await setup();
    const callsBefore = search.mock.calls.length;

    actions.complete(resultType);
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

  it('openFiles_WithMachineId_NavigatesToTheFilesRoute', async () => {
    const { store } = await setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    store.openFiles(MACHINE.id);

    expect(navigate).toHaveBeenCalledWith(['/machines', MACHINE.id, 'files']);
  });

  it('canReadFiles_WithoutPermission_IsFalse', async () => {
    const { store } = await setup({ hasPermission: false });

    expect(store.canReadFiles()).toBe(false);
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
});
