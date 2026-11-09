import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';

import type { FileGridItem } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';

import { FileEditStore } from './file-edit.store';

const MACHINE_ID = 'encrypted-machine-id';
const ITEM: FileGridItem = {
  id: 'file-id',
  fileName: 'old',
  extension: 'pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  description: null,
  uploadedAt: '2026-03-01T08:00:00Z',
  rowVersion: 42,
};

describe('FileEditStore', () => {
  function setup(scope: object): {
    store: InstanceType<typeof FileEditStore>;
    http: HttpTestingController;
    actions: { complete: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    messages: { add: ReturnType<typeof vi.fn> };
  } {
    const actions = { complete: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        FileEditStore,
        FilesApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(FileEditStore);
    store.start({ type: FileActionType.Edit, payload: { scope, item: ITEM } });
    return { store, http: TestBed.inject(HttpTestingController), actions, messages };
  }

  it('initialValue_WithItem_ShowsTheBaseNameAndAnEmptyDescription', () => {
    const { store } = setup({ kind: 'company' });

    expect(store.initialValue()).toEqual({ fileName: 'old', description: '' });
    expect(store.extension()).toBe('pdf');
  });

  it('save_WithCompanyScope_PutsToTheCompanyRouteWithTheRowVersion', () => {
    const { store, http, actions } = setup({ kind: 'company' });

    store.save({ fileName: 'new', description: 'text' });
    const request = http.expectOne('/api/document/v1/files/file-id');
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ fileName: 'new', description: 'text', rowVersion: 42 });
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
  });

  it('save_WithMachineScopeAndEmptyDescription_PutsToTheMachineRouteWithNullDescription', () => {
    const { store, http } = setup({ kind: 'machine', machineId: MACHINE_ID });

    store.save({ fileName: 'new', description: '' });
    const request = http.expectOne(`/api/document/v1/machines/${MACHINE_ID}/files/file-id`);
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.body).toEqual({ fileName: 'new', description: null, rowVersion: 42 });
  });

  it.each([
    [HttpStatusCode.Conflict, 'FILES.ERRORS.CONFLICT'],
    [HttpStatusCode.NotFound, 'FILES.ERRORS.NOT_FOUND'],
  ])('save_WithStatus%d_ShowsTheMessageAndClosesWithSavedSoTheGridReloads', (status, key) => {
    const { store, http, actions, messages } = setup({ kind: 'company' });

    store.save({ fileName: 'new', description: '' });
    http.expectOne('/api/document/v1/files/file-id').flush(null, { status, statusText: String(status) });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: key }));
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
    expect(store.isSaving()).toBe(false);
  });

  it('save_WithServerError_KeepsTheDialogOpen', () => {
    const { store, http, actions, messages } = setup({ kind: 'company' });

    store.save({ fileName: 'new', description: '' });
    http
      .expectOne('/api/document/v1/files/file-id')
      .flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Error' });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'ERRORS.HTTP.SERVER' }));
    expect(actions.complete).not.toHaveBeenCalled();
  });
});
