import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import type { MachineErrorCode } from '@machines/error-codes/machine-error-code';
import { MachineErrorCodesApi } from '@machines/error-codes/machine-error-codes-api';

import { MachineErrorCodeDrawerStore } from './machine-error-code-drawer.store';
import type { MachineErrorCodeFormValue } from './machine-error-code-form-schema';

const MACHINE_ID = 'encrypted-machine-id';

const ERROR_CODE: MachineErrorCode = {
  id: 'encrypted-id',
  code: 'E-101',
  description: '<p>old</p>',
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
};

const VALUE: MachineErrorCodeFormValue = { code: ' E-202 ', description: '<p>new</p>' };
const TRIMMED_BODY = { code: 'E-202', description: '<p>new</p>' };

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MachineErrorCodeDrawerStore>;
  api: { get: Spy; create: Spy; update: Spy };
  saved: ReturnType<typeof vi.fn<() => void>>;
  messages: { add: Spy };
}

function httpError(status: number): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: String(status) });
}

function setup(overrides: Partial<Setup['api']> = {}): Setup {
  const messages = { add: vi.fn() };
  const api = {
    get: vi.fn(() => of(ERROR_CODE)),
    create: vi.fn(() => of({ id: 'new-id' })),
    update: vi.fn(() => of(undefined)),
    ...overrides,
  };
  TestBed.configureTestingModule({
    providers: [
      MachineErrorCodeDrawerStore,
      { provide: MachineErrorCodesApi, useValue: api },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const store = TestBed.inject(MachineErrorCodeDrawerStore);
  const saved = vi.fn<() => void>();
  return { store, api, saved, messages };
}

describe('MachineErrorCodeDrawerStore', () => {
  it('save_WithNewErrorCode_CreatesWithTrimmedBodyAndEmitsSaved', () => {
    const { store, api, saved, messages } = setup();
    store.start(MACHINE_ID, null);

    store.save({ value: VALUE, onSaved: saved });

    expect(api.create).toHaveBeenCalledWith(MACHINE_ID, TRIMMED_BODY, expect.anything());
    expect(api.update).not.toHaveBeenCalled();
    expect(saved).toHaveBeenCalledTimes(1);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('save_WithExistingErrorCode_UpdatesAndEmitsSaved', () => {
    const { store, api, saved } = setup();
    store.start(MACHINE_ID, ERROR_CODE.id);

    store.save({ value: VALUE, onSaved: saved });

    expect(api.update).toHaveBeenCalledWith(MACHINE_ID, ERROR_CODE.id, TRIMMED_BODY, expect.anything());
    expect(api.create).not.toHaveBeenCalled();
    expect(saved).toHaveBeenCalledTimes(1);
  });

  it('save_WithConflict_ShowsCodeTakenToastAndEmitsNoSaved', () => {
    const { store, saved, messages } = setup({
      create: vi.fn(() => throwError(() => httpError(HttpStatusCode.Conflict))),
    });
    store.start(MACHINE_ID, null);

    store.save({ value: VALUE, onSaved: saved });

    expect(saved).not.toHaveBeenCalled();
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'MACHINE_ERROR_CODES.TOASTS.CODE_TAKEN' }),
    );
    expect(store.isSaving()).toBe(false);
  });

  it('save_WithOtherFailure_ShowsStatusMessageInsteadOfCodeTaken', () => {
    const { store, saved, messages } = setup({
      create: vi.fn(() => throwError(() => httpError(HttpStatusCode.InternalServerError))),
    });
    store.start(MACHINE_ID, null);

    store.save({ value: VALUE, onSaved: saved });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
    expect(messages.add).not.toHaveBeenCalledWith(
      expect.objectContaining({ detail: 'MACHINE_ERROR_CODES.TOASTS.CODE_TAKEN' }),
    );
  });

  it('initialValue_ForNewErrorCode_IsEmptyAndForExistingLoadsFromApi', () => {
    const { store, api } = setup();

    store.start(MACHINE_ID, null);
    const createValue = store.initialValue();
    store.start(MACHINE_ID, ERROR_CODE.id);
    TestBed.tick();

    expect(createValue).toEqual({ code: '', description: null });
    expect(api.get).toHaveBeenCalledWith(MACHINE_ID, ERROR_CODE.id);
  });
});
