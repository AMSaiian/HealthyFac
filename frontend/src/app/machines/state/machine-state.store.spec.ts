import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, Subject, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { type Machine, MachineState } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { MachineStateStore } from './machine-state.store';

const MACHINE: Machine = {
  id: 'encrypted-id',
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  gatewayId: 'abcdefghijklmnop',
  state: MachineState.Operational,
  description: '<p>old</p>',
  expectedGoLiveOn: '2099-01-01',
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
  pendingDraftCount: 0,
};

const ACTION = { type: MachineActionType.ChangeState, payload: { machineId: MACHINE.id } };

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MachineStateStore>;
  api: { get: Spy; changeState: Spy };
  complete: ReturnType<typeof vi.spyOn>;
  messages: { add: Spy };
}

function setup(overrides: Partial<Setup['api']> = {}): Setup {
  const messages = { add: vi.fn() };
  const api = {
    get: vi.fn(() => of(MACHINE)),
    changeState: vi.fn(() => of(undefined)),
    ...overrides,
  };
  TestBed.configureTestingModule({
    providers: [
      MachineStateStore,
      { provide: MachinesApi, useValue: api },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const complete = vi.spyOn(TestBed.inject(ActionStore), 'complete');
  return { store: TestBed.inject(MachineStateStore), api, complete, messages };
}

async function load(store: InstanceType<typeof MachineStateStore>): Promise<void> {
  store.start(ACTION);
  TestBed.tick();
  await TestBed.inject(ApplicationRef).whenStable();
}

describe('MachineStateStore', () => {
  it('selectedState_AfterLoad_IsTheCurrentStateAndSaveIsDisabled', async () => {
    const { store } = setup();

    await load(store);

    expect(store.selectedState()).toBe(MachineState.Operational);
    expect(store.saveDisabledReason()).toBe('FORMS.NO_CHANGES');
  });

  it('saveDisabledReason_BeforeTheMachineLoads_IsNoChanges', () => {
    const { store } = setup({ get: vi.fn(() => new Subject<Machine>()) });

    store.start(ACTION);
    TestBed.tick();

    expect(store.saveDisabledReason()).toBe('FORMS.NO_CHANGES');
  });

  it('saveDisabledReason_WithAnotherState_IsNullAndWithTheCurrentStateAgainIsNoChanges', async () => {
    const { store } = setup();
    await load(store);

    store.select(MachineState.InMaintenance);
    const withAnother = store.saveDisabledReason();
    store.select(MachineState.Operational);

    expect(withAnother).toBeNull();
    expect(store.saveDisabledReason()).toBe('FORMS.NO_CHANGES');
  });

  it('save_WithAnotherState_PostsTheStateOnceShowsTheToastAndCompletesSaved', async () => {
    const { store, api, complete, messages } = setup();
    await load(store);
    store.select(MachineState.InMaintenance);

    store.save();

    expect(api.changeState).toHaveBeenCalledTimes(1);
    expect(api.changeState).toHaveBeenCalledWith(
      MACHINE.id,
      { state: MachineState.InMaintenance },
      expect.objectContaining({ context: expect.anything() }),
    );
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'MACHINES.TOASTS.STATE_CHANGED' }));
    expect(complete).toHaveBeenCalledWith(MachineActionType.Saved);
    expect(store.isSaving()).toBe(false);
  });

  it('save_WithTheCurrentState_SendsNothing', async () => {
    const { store, api, complete } = setup();
    await load(store);

    store.save();

    expect(api.changeState).not.toHaveBeenCalled();
    expect(complete).not.toHaveBeenCalled();
  });

  it('save_WithAFailedPost_ShowsAnErrorToastAndKeepsTheDialogOpen', async () => {
    const failure = new HttpErrorResponse({ status: HttpStatusCode.InternalServerError });
    const { store, complete, messages } = setup({ changeState: vi.fn(() => throwError(() => failure)) });
    await load(store);
    store.select(MachineState.OutOfService);

    store.save();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(complete).not.toHaveBeenCalled();
    expect(store.isSaving()).toBe(false);
  });

  it('save_CalledTwiceWhileSaving_SendsOneRequest', async () => {
    const pending = new Subject<void>();
    const { store, api } = setup({ changeState: vi.fn(() => pending) });
    await load(store);
    store.select(MachineState.InMaintenance);

    store.save();
    store.save();
    pending.next();
    pending.complete();

    expect(api.changeState).toHaveBeenCalledTimes(1);
  });
});
