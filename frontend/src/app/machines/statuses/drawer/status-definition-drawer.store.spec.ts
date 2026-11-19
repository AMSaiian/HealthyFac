import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { DetailsMode } from '@shared/forms/with-details-mode';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';
import { type StatusDefinition, StatusSeverity } from '@machines/statuses/status-definition';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

import { StatusDefinitionDrawerStore } from './status-definition-drawer.store';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';

const MACHINE_ID = 'encrypted-machine-id';

const STATUS_DEFINITION: StatusDefinition = {
  id: 'encrypted-id',
  code: 'E-101',
  severity: StatusSeverity.Warning,
  description: '<p>old</p>',
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
};

const VALUE: StatusDefinitionFormValue = { code: ' E-202 ', severity: StatusSeverity.Error, description: '<p>new</p>' };
const TRIMMED_BODY = { code: 'E-202', severity: StatusSeverity.Error, description: '<p>new</p>' };

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof StatusDefinitionDrawerStore>;
  api: { get: Spy; create: Spy; update: Spy };
  saved: ReturnType<typeof vi.fn<() => void>>;
  messages: { add: Spy };
}

function httpError(status: number): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: String(status) });
}

function setup(
  overrides: Partial<Setup['api']> = {},
  permissions: readonly Permission[] = [Permission.StatusDefinitionsWrite],
): Setup {
  const messages = { add: vi.fn() };
  const api = {
    get: vi.fn(() => of(STATUS_DEFINITION)),
    create: vi.fn(() => of({ id: 'new-id' })),
    update: vi.fn(() => of(undefined)),
    ...overrides,
  };
  TestBed.configureTestingModule({
    providers: [
      StatusDefinitionDrawerStore,
      { provide: StatusDefinitionsApi, useValue: api },
      { provide: MessageService, useValue: messages },
      { provide: AuthStore, useValue: { hasPermission: (permission: Permission) => permissions.includes(permission) } },
      { provide: RichTextImageLinks, useValue: { resolve: () => of(new Map<string, string>()) } },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const store = TestBed.inject(StatusDefinitionDrawerStore);
  const saved = vi.fn<() => void>();
  return { store, api, saved, messages };
}

describe('StatusDefinitionDrawerStore', () => {
  it('save_WithNewStatusDefinition_CreatesWithTrimmedBodyAndEmitsSaved', () => {
    const { store, api, saved, messages } = setup();
    store.start(MACHINE_ID, null);

    store.save({ value: VALUE, onSaved: saved });

    expect(api.create).toHaveBeenCalledWith(MACHINE_ID, TRIMMED_BODY, expect.anything());
    expect(api.update).not.toHaveBeenCalled();
    expect(saved).toHaveBeenCalledTimes(1);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('save_WithExistingStatusDefinition_UpdatesAndEmitsSaved', () => {
    const { store, api, saved } = setup();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);

    store.save({ value: VALUE, onSaved: saved });

    expect(api.update).toHaveBeenCalledWith(MACHINE_ID, STATUS_DEFINITION.id, TRIMMED_BODY, expect.anything());
    expect(api.create).not.toHaveBeenCalled();
    expect(saved).toHaveBeenCalledTimes(1);
  });

  it('start_ForExisting_IsViewMode', () => {
    const { store } = setup();

    store.start(MACHINE_ID, STATUS_DEFINITION.id);

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('start_ForNew_IsEditMode', () => {
    const { store } = setup();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);

    store.start(MACHINE_ID, null);

    expect(store.mode()).toBe(DetailsMode.Edit);
  });

  it('isEditShown_WithoutWritePermission_IsFalse', async () => {
    const { store } = setup({}, [Permission.StatusDefinitionsRead]);
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.isEditShown()).toBe(false);
  });

  it('isEditShown_WithWritePermissionAfterLoad_IsTrue', async () => {
    const { store } = setup();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    const beforeLoad = store.isEditShown();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(beforeLoad).toBe(false);
    expect(store.isEditShown()).toBe(true);
  });

  it('save_WithExistingStatusDefinition_ReloadsAndReturnsToView', async () => {
    const { store, api, saved } = setup();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    store.edit();

    store.save({ value: VALUE, onSaved: saved });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(api.get).toHaveBeenCalledTimes(2);
    expect(store.mode()).toBe(DetailsMode.View);
    expect(saved).toHaveBeenCalledTimes(1);
  });

  it('save_WithConflict_StaysInEditMode', async () => {
    const { store, saved } = setup({ update: vi.fn(() => throwError(() => httpError(HttpStatusCode.Conflict))) });
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    store.edit();

    store.save({ value: VALUE, onSaved: saved });

    expect(store.mode()).toBe(DetailsMode.Edit);
    expect(saved).not.toHaveBeenCalled();
  });

  it('cancelEdit_ForExisting_ReturnsToView', () => {
    const { store } = setup();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    store.edit();

    store.cancelEdit();

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('save_WithConflict_ShowsCodeTakenToastAndEmitsNoSaved', () => {
    const { store, saved, messages } = setup({
      create: vi.fn(() => throwError(() => httpError(HttpStatusCode.Conflict))),
    });
    store.start(MACHINE_ID, null);

    store.save({ value: VALUE, onSaved: saved });

    expect(saved).not.toHaveBeenCalled();
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'STATUS_DEFINITIONS.TOASTS.CODE_TAKEN' }),
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
      expect.objectContaining({ detail: 'STATUS_DEFINITIONS.TOASTS.CODE_TAKEN' }),
    );
  });

  it('initialValue_ForNewStatusDefinition_IsEmptyAndForExistingLoadsFromApi', () => {
    const { store, api } = setup();

    store.start(MACHINE_ID, null);
    const createValue = store.initialValue();
    store.start(MACHINE_ID, STATUS_DEFINITION.id);
    TestBed.tick();

    expect(createValue).toEqual({ code: '', severity: StatusSeverity.Info, description: null });
    expect(api.get).toHaveBeenCalledWith(MACHINE_ID, STATUS_DEFINITION.id);
  });
});
