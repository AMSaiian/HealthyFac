import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { type Machine, MachineState } from '@machines/machine';
import { MachineActionType } from '@machines/machine-actions';
import { MachinesApi } from '@machines/machines-api';

import { MachineDrawerStore } from './machine-drawer.store';
import type { MachineFormValue } from './machine-form-schema';

const MACHINE: Machine = {
  id: 'encrypted-id',
  name: 'Lathe 01',
  serialNumber: 'ABC-1',
  departmentId: 'department-1',
  state: MachineState.Operational,
  description: '<p>old</p>',
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
};

const VALUE: MachineFormValue = {
  name: ' Lathe 02 ',
  serialNumber: 'XYZ-2 ',
  departmentId: 'department-2',
  description: '<p>new</p>',
};

const TRIMMED_BODY = {
  name: 'Lathe 02',
  serialNumber: 'XYZ-2',
  departmentId: 'department-2',
  description: '<p>new</p>',
};

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MachineDrawerStore>;
  api: { get: Spy; create: Spy; update: Spy };
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
  TestBed.configureTestingModule({
    providers: [
      MachineDrawerStore,
      { provide: MachinesApi, useValue: api },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const complete = vi.spyOn(TestBed.inject(ActionStore), 'complete');
  return { store: TestBed.inject(MachineDrawerStore), api, complete, messages };
}

describe('MachineDrawerStore', () => {
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

    store.save(VALUE);

    expect(api.update).toHaveBeenCalledWith(
      MACHINE.id,
      TRIMMED_BODY,
      expect.objectContaining({ context: expect.anything() }),
    );
    expect(complete).toHaveBeenCalledWith(MachineActionType.Saved);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'MACHINES.TOASTS.UPDATED' }));
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
      departmentId: MACHINE.departmentId,
      description: MACHINE.description,
    });
    expect(store.prefilledValue()).toEqual({
      name: 'Prefilled',
      serialNumber: MACHINE.serialNumber,
      departmentId: MACHINE.departmentId,
      description: MACHINE.description,
    });
  });

  it('start_ForCreate_PrefillsFromPayload', () => {
    const { store } = setup();

    store.start({ type: MachineActionType.Create, payload: { name: 'Agent name' } });

    expect(store.prefilledValue()).toEqual({
      name: 'Agent name',
      serialNumber: '',
      departmentId: '',
      description: null,
    });
  });
});
