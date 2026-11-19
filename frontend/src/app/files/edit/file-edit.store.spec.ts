import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { DetailsMode } from '@shared/forms/with-details-mode';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

import type { FileGridItem } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';

import { FileEditStore } from './file-edit.store';

const MACHINE_ID = 'encrypted-machine-id';
const COMPANY_URL = '/api/document/v1/files/file-id';
const MACHINE_URL = `/api/document/v1/machines/${MACHINE_ID}/files/file-id`;
const ITEM: FileGridItem = {
  id: 'file-id',
  fileName: 'old',
  extension: 'pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  description: null,
  uploadedAt: '2026-03-01T08:00:00Z',
  machineId: null,
  machineName: null,
  ingestState: null,
  canIngest: true,
  rowVersion: 42,
};

interface Setup {
  store: InstanceType<typeof FileEditStore>;
  http: HttpTestingController;
  actions: { complete: ReturnType<typeof vi.fn>; notify: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
  messages: { add: ReturnType<typeof vi.fn> };
}

describe('FileEditStore', () => {
  function setup(
    scope: object,
    permissions: readonly Permission[] = [Permission.CompanyFilesWrite, Permission.FilesWrite],
  ): Setup {
    const actions = { complete: vi.fn(), notify: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        FileEditStore,
        FilesApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        {
          provide: AuthStore,
          useValue: { hasPermission: (permission: Permission) => permissions.includes(permission) },
        },
        { provide: RichTextImageLinks, useValue: { resolve: () => of(new Map<string, string>()) } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(FileEditStore);
    store.start({ type: FileActionType.Edit, payload: { scope, id: ITEM.id } });
    TestBed.tick();
    return { store, http: TestBed.inject(HttpTestingController), actions, messages };
  }

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function loaded(scope: object, permissions?: readonly Permission[]): Promise<Setup> {
    const result = setup(scope, permissions);
    result.http.expectOne(COMPANY_URL).flush(ITEM);
    await settle();
    return result;
  }

  it('start_LoadsTheFileFromTheCompanyRoute', async () => {
    const { http } = setup({ kind: 'company' });

    const request = http.expectOne(COMPANY_URL);

    expect(request.request.method).toBe('GET');
  });

  it('start_LoadsTheFileFromTheMachineRoute', async () => {
    const { http } = setup({ kind: 'machine', machineId: MACHINE_ID });

    const request = http.expectOne(MACHINE_URL);

    expect(request.request.method).toBe('GET');
  });

  it('start_IsViewMode', async () => {
    const { store } = setup({ kind: 'company' });

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('initialValue_WithLoadedFile_ShowsTheBaseNameAndANullDescription', async () => {
    const { store } = await loaded({ kind: 'company' });

    expect(store.initialValue()).toEqual({ fileName: 'old', description: null });
    expect(store.extension()).toBe('pdf');
  });

  it('start_WhenTheFileIsNotFound_ClosesTheDialog', async () => {
    const { http, actions } = setup({ kind: 'company' });

    http
      .expectOne(COMPANY_URL)
      .flush(null, { status: HttpStatusCode.NotFound, statusText: String(HttpStatusCode.NotFound) });
    await settle();

    expect(actions.cancel).toHaveBeenCalledTimes(1);
  });

  it('isEditShown_WithoutWriteRightOfTheScope_IsFalse', async () => {
    const { store } = await loaded({ kind: 'company' }, [Permission.FilesWrite]);

    expect(store.isEditShown()).toBe(false);
  });

  it('isEditShown_WithWriteRightOfTheScopeAfterLoad_IsTrue', async () => {
    const { store, http } = setup({ kind: 'machine', machineId: MACHINE_ID }, [Permission.FilesWrite]);
    const beforeLoad = store.isEditShown();
    http.expectOne(MACHINE_URL).flush(ITEM);
    await settle();

    expect(beforeLoad).toBe(false);
    expect(store.isEditShown()).toBe(true);
  });

  it('save_SendsTheRowVersionOfTheLoadedFile', async () => {
    const { store, http } = await loaded({ kind: 'company' });

    store.save({ fileName: 'new', description: 'text' });
    const request = http.expectOne(COMPANY_URL);
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ fileName: 'new', description: 'text', rowVersion: 42 });
  });

  it('save_WithMachineScopeAndNullDescription_PutsToTheMachineRouteWithNullDescription', async () => {
    const { store, http } = setup({ kind: 'machine', machineId: MACHINE_ID });
    http.expectOne(MACHINE_URL).flush(ITEM);
    await settle();

    store.save({ fileName: 'new', description: null });
    const request = http.expectOne(MACHINE_URL);
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ fileName: 'new', description: null, rowVersion: 42 });
  });

  it('save_WithSuccess_NotifiesReloadsAndReturnsToView', async () => {
    const { store, http, actions } = await loaded({ kind: 'company' });
    store.edit();

    store.save({ fileName: 'new', description: 'text' });
    http.expectOne(COMPANY_URL).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    TestBed.tick();
    const reload = http.expectOne(COMPANY_URL);

    expect(reload.request.method).toBe('GET');
    expect(actions.notify).toHaveBeenCalledWith(FileActionType.Saved);
    expect(actions.complete).not.toHaveBeenCalled();
    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('save_Twice_SendsTheReloadedRowVersion', async () => {
    const { store, http } = await loaded({ kind: 'company' });
    store.edit();
    store.save({ fileName: 'first', description: null });
    http.expectOne(COMPANY_URL).flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    TestBed.tick();
    http.expectOne(COMPANY_URL).flush({ ...ITEM, fileName: 'first', rowVersion: 43 });
    await settle();
    store.edit();

    store.save({ fileName: 'second', description: null });
    const request = http.expectOne(COMPANY_URL);
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.body).toEqual({ fileName: 'second', description: null, rowVersion: 43 });
  });

  it.each([
    [HttpStatusCode.Conflict, 'FILES.ERRORS.CONFLICT'],
    [HttpStatusCode.NotFound, 'FILES.ERRORS.NOT_FOUND'],
  ])('save_With409Or404_Status%d_ShowsTheMessageAndClosesWithSavedSoTheGridReloads', async (status, key) => {
    const { store, http, actions, messages } = await loaded({ kind: 'company' });
    store.edit();

    store.save({ fileName: 'new', description: '' });
    http.expectOne(COMPANY_URL).flush(null, { status, statusText: String(status) });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: key }));
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
    expect(store.isSaving()).toBe(false);
  });

  it('save_WithServerError_KeepsTheDialogOpenInEditMode', async () => {
    const { store, http, actions, messages } = await loaded({ kind: 'company' });
    store.edit();

    store.save({ fileName: 'new', description: '' });
    http.expectOne(COMPANY_URL).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Error' });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'ERRORS.HTTP.SERVER' }));
    expect(actions.complete).not.toHaveBeenCalled();
    expect(actions.notify).not.toHaveBeenCalled();
    expect(store.mode()).toBe(DetailsMode.Edit);
  });

  it('cancelEdit_ReturnsToView', async () => {
    const { store } = await loaded({ kind: 'company' });
    store.edit();

    store.cancelEdit();

    expect(store.mode()).toBe(DetailsMode.View);
  });
});
