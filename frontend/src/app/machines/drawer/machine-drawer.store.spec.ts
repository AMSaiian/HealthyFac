import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';
import { type Machine, MachineState } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachinesApi } from '@machines/machines-api';

import { MachineDrawerStore } from './machine-drawer.store';
import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

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

const VALUE: MachineFormValue = {
  name: ' Lathe 02 ',
  serialNumber: 'XYZ-2 ',
  description: '<p>new</p>',
  expectedGoLiveOn: Temporal.PlainDate.from('2026-03-11'),
  state: null,
};

const UPDATE_VALUE: MachineFormValue = { ...VALUE, state: MachineState.InMaintenance };

const TODAY = Temporal.PlainDate.from('2026-03-11');

const UPDATE_BODY = {
  name: 'Lathe 02',
  serialNumber: 'XYZ-2',
  description: '<p>new</p>',
  expectedGoLiveOn: '2026-03-11',
};

const TRIMMED_BODY = UPDATE_BODY;

const STATE_UPDATE_BODY = { ...UPDATE_BODY, state: MachineState.InMaintenance };

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MachineDrawerStore>;
  api: { get: Spy; create: Spy; update: Spy };
  draftsApi: { getMachineDraft: Spy; applyMachineDraft: Spy };
  complete: ReturnType<typeof vi.spyOn>;
  messages: { add: Spy };
}

function httpError(status: number): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: String(status) });
}

function setup(overrides: Partial<Setup['api']> = {}): Setup {
  const messages = { add: vi.fn() };
  const api = {
    get: vi.fn(() => of(MACHINE)),
    create: vi.fn(() => of({ id: 'new-id' })),
    update: vi.fn(() => of(undefined)),
    ...overrides,
  };
  const draftsApi = {
    getMachineDraft: vi.fn(() =>
      of({
        draft: {
          id: 'draft-id',
          isCreate: true,
          name: 'Pump',
          serialNumber: null,
          description: null,
        },
        current: null,
      }),
    ),
    applyMachineDraft: vi.fn(() => of({ machineId: 'new-id' })),
  };
  TestBed.configureTestingModule({
    providers: [
      MachineDrawerStore,
      { provide: MachinesApi, useValue: api },
      { provide: DraftsApi, useValue: draftsApi },
      { provide: MessageService, useValue: messages },
      {
        provide: CLOCK,
        useValue: { now: () => Temporal.Instant.from('2026-03-10T23:30:00Z'), timeZoneId: () => 'Europe/Kyiv' },
      },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const complete = vi.spyOn(TestBed.inject(ActionStore), 'complete');
  return { store: TestBed.inject(MachineDrawerStore), api, draftsApi, complete, messages };
}

describe('MachineDrawerStore', () => {
  it('load_ForCreateDraft_PrefillsTheDraftValuesAndEmptiesMissingOnes', async () => {
    const { store, draftsApi } = setup();
    store.start({ type: MachineActionType.Create, payload: { draftId: 'draft-id' } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(draftsApi.getMachineDraft).toHaveBeenCalledWith('draft-id');
    expect(store.initialValue()).toEqual({
      name: 'Pump',
      serialNumber: '',
      description: null,
      expectedGoLiveOn: TODAY,
      state: null,
    });
  });

  it('save_ForCreateDraft_AppliesTheDraftAndCompletesChangedAndSaved', async () => {
    const { store, api, draftsApi, complete } = setup();
    store.start({ type: MachineActionType.Create, payload: { draftId: 'draft-id' } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.save(VALUE);

    expect(draftsApi.applyMachineDraft).toHaveBeenCalledWith(
      'draft-id',
      TRIMMED_BODY,
      expect.objectContaining({ context: expect.anything() }),
    );
    expect(api.create).not.toHaveBeenCalled();
    expect(complete).toHaveBeenCalledTimes(1);
    expect(complete).toHaveBeenCalledWith(MachineActionType.Saved);
  });

  it('save_WithSuccess_CompletesAction', () => {
    const { store, api, complete, messages } = setup();
    store.start({ type: MachineActionType.Create, payload: {} });

    store.save(VALUE);

    expect(api.create).toHaveBeenCalledWith(TRIMMED_BODY, expect.objectContaining({ context: expect.anything() }));
    expect(complete).toHaveBeenCalledWith(MachineActionType.Saved);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'MACHINES.TOASTS.CREATED' }));
  });

  it('save_ForUpdate_CallsUpdateAndCompletesAction', async () => {
    const { store, api, complete, messages } = setup();
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.save(UPDATE_VALUE);

    expect(api.update).toHaveBeenCalledWith(
      MACHINE.id,
      STATE_UPDATE_BODY,
      expect.objectContaining({ context: expect.anything() }),
    );
    expect(complete).toHaveBeenCalledWith(MachineActionType.Saved);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'MACHINES.TOASTS.UPDATED' }));
  });

  it('save_ForUpdateWithOnlyAnotherState_SendsTheStateAndTheUnchangedFields', async () => {
    const { store, api } = setup();
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    const initial = store.initialValue() as MachineFormValue;

    store.save({ ...initial, state: MachineState.OutOfService });

    expect(api.update).toHaveBeenCalledWith(
      MACHINE.id,
      {
        name: MACHINE.name,
        serialNumber: MACHINE.serialNumber,
        description: MACHINE.description,
        expectedGoLiveOn: MACHINE.expectedGoLiveOn,
        state: MachineState.OutOfService,
      },
      expect.objectContaining({ context: expect.anything() }),
    );
  });

  it('save_ForUpdateWithoutState_ShowsAnErrorToastAndSendsNoRequest', async () => {
    const { store, api, messages } = setup();
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.save({ ...(store.initialValue() as MachineFormValue), state: null });

    expect(api.update).not.toHaveBeenCalled();
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('save_ForCreate_SendsABodyWithoutState', () => {
    const { store, api } = setup();
    store.start({ type: MachineActionType.Create, payload: {} });

    store.save(VALUE);

    expect(api.create.mock.calls[0][0]).not.toHaveProperty('state');
  });

  it('save_ForCreateDraft_SendsAnApplyBodyWithoutState', async () => {
    const { store, draftsApi } = setup();
    store.start({ type: MachineActionType.Create, payload: { draftId: 'draft-id' } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.save(VALUE);

    expect(draftsApi.applyMachineDraft.mock.calls[0][1]).not.toHaveProperty('state');
  });

  it('save_WithConflict_ShowsSerialNumberToastAndKeepsDrawerOpen', () => {
    const { store, complete, messages } = setup({
      create: vi.fn(() => throwError(() => httpError(HttpStatusCode.Conflict))),
    });
    store.start({ type: MachineActionType.Create, payload: {} });

    store.save(VALUE);

    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'MACHINES.TOASTS.SERIAL_NUMBER_TAKEN' }),
    );
    expect(complete).not.toHaveBeenCalled();
    expect(store.isSaving()).toBe(false);
  });

  it('save_WithServerError_ShowsHttpErrorToast', () => {
    const { store, messages } = setup({
      create: vi.fn(() => throwError(() => httpError(HttpStatusCode.InternalServerError))),
    });
    store.start({ type: MachineActionType.Create, payload: {} });

    store.save(VALUE);

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'ERRORS.HTTP.SERVER' }));
  });

  it('load_ForUpdate_AppliesPayloadOverLoadedMachine', async () => {
    const { store, api } = setup();
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id, name: 'Prefilled' } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(api.get).toHaveBeenCalledWith(MACHINE.id);
    expect(store.initialValue()).toEqual({
      name: MACHINE.name,
      serialNumber: MACHINE.serialNumber,
      description: MACHINE.description,
      expectedGoLiveOn: Temporal.PlainDate.from(MACHINE.expectedGoLiveOn),
      state: MACHINE.state,
    });
    expect(store.prefilledValue()).toEqual({
      name: 'Prefilled',
      serialNumber: MACHINE.serialNumber,
      description: MACHINE.description,
      expectedGoLiveOn: Temporal.PlainDate.from(MACHINE.expectedGoLiveOn),
      state: MACHINE.state,
    });
  });

  it('load_ForUpdate_ExposesGatewayId', async () => {
    const { store } = setup();
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.gatewayId()).toBe(MACHINE.gatewayId);
  });

  it('start_ForCreate_PrefillsFromPayload', () => {
    const { store } = setup();

    store.start({ type: MachineActionType.Create, payload: { name: 'Agent name' } });

    expect(store.prefilledValue()).toEqual({
      name: 'Agent name',
      serialNumber: '',
      description: null,
      expectedGoLiveOn: TODAY,
      state: null,
    });
  });

  it('minGoLiveOn_ForCreate_IsTodayInCompanyZone', () => {
    const { store } = setup();
    store.start({ type: MachineActionType.Create, payload: {} });

    expect(store.minGoLiveOn()).toEqual(TODAY);
  });

  it('minGoLiveOn_ForCreateDraft_IsTodayInCompanyZone', async () => {
    const { store } = setup();
    store.start({ type: MachineActionType.Create, payload: { draftId: 'draft-id' } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.minGoLiveOn()).toEqual(TODAY);
  });

  it('minGoLiveOn_ForUpdate_IsCreationDayInCompanyZone', async () => {
    const { store } = setup({ get: vi.fn(() => of({ ...MACHINE, createdAt: '2026-03-04T22:30:00Z' })) });
    store.start({ type: MachineActionType.Update, payload: { machineId: MACHINE.id } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.minGoLiveOn()).toEqual(Temporal.PlainDate.from('2026-03-05'));
  });

  it('overlay_WithPayloadDate_KeepsTheBaseDate', () => {
    const { store } = setup();
    store.start({
      type: MachineActionType.Create,
      payload: { name: 'Agent name', expectedGoLiveOn: '2030-01-01' } as never,
    });

    expect(store.prefilledValue()?.expectedGoLiveOn).toEqual(TODAY);
  });
});
