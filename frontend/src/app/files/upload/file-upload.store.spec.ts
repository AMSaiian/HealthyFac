import { HttpEventType, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';

import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';

import { FileUploadStore } from './file-upload.store';
import { UploadStatus } from './file-upload.store.utils';

const MACHINE_ID = 'encrypted-machine-id';
const COMPANY_URL = '/api/document/v1/files';

function file(name: string, size: number): File {
  return new File([new Uint8Array(size)], name);
}

describe('FileUploadStore', () => {
  function setup(): {
    store: InstanceType<typeof FileUploadStore>;
    http: HttpTestingController;
    actions: { complete: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    messages: { add: ReturnType<typeof vi.fn> };
  } {
    const actions = { complete: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        FileUploadStore,
        FilesApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    return { store: TestBed.inject(FileUploadStore), http: TestBed.inject(HttpTestingController), actions, messages };
  }

  it('addFiles_WithRefusedTypeAndSize_NeverCallTheApi', () => {
    const { store, http } = setup();

    store.addFiles([file('a.zip', 10), file('big.pdf', 104_857_601), file('empty.pdf', 0)]);
    store.upload();

    expect(store.items().map((item) => item.status)).toEqual([
      UploadStatus.Refused,
      UploadStatus.Refused,
      UploadStatus.Refused,
    ]);
    expect(store.items().map((item) => item.errorKey)).toEqual([
      'FILES.ERRORS.TYPE_NOT_ALLOWED',
      'FILES.ERRORS.TOO_LARGE',
      'FILES.ERRORS.EMPTY',
    ]);
    http.expectNone(COMPANY_URL);
  });

  it('upload_WithAcceptedFile_SendsFormWithDescriptionAndUpdatesProgress', () => {
    const { store, http } = setup();
    store.addFiles([file('manual.pdf', 10)]);
    store.setDescription(0, 'My manual');

    store.upload();
    const request = http.expectOne(COMPANY_URL);
    request.event({ type: HttpEventType.UploadProgress, loaded: 5, total: 10 });

    const body = request.request.body as FormData;
    expect(body.get('description')).toBe('My manual');
    expect((body.get('file') as File).name).toBe('manual.pdf');
    expect(store.items()[0].progress).toBe(50);
    expect(store.items()[0].status).toBe(UploadStatus.Uploading);
  });

  it('upload_WithMachineScope_UsesTheMachineUrl', () => {
    const { store, http } = setup();
    store.start({ type: FileActionType.Upload, payload: { scope: { kind: 'machine', machineId: MACHINE_ID } } });
    store.addFiles([file('a.pdf', 10)]);

    store.upload();

    http.expectOne(`/api/document/v1/machines/${MACHINE_ID}/files`);
  });

  it('upload_WithFourFiles_SendsAtMostThreeAtOnce', () => {
    const { store, http } = setup();
    store.addFiles([file('a.pdf', 1), file('b.pdf', 1), file('c.pdf', 1), file('d.pdf', 1)]);

    store.upload();

    expect(http.match(COMPANY_URL)).toHaveLength(3);
  });

  it('upload_WhenAllSucceed_ShowsToastAndCompletesWithSaved', () => {
    const { store, http, actions, messages } = setup();
    store.addFiles([file('a.pdf', 10)]);

    store.upload();
    http.expectOne(COMPANY_URL).flush({ id: 'new-id' });

    expect(store.items()[0].status).toBe(UploadStatus.Done);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
  });

  it.each([
    [HttpStatusCode.BadRequest, { errors: { file: ['fileTypeNotAllowed'] } }, 'FILES.ERRORS.TYPE_NOT_ALLOWED'],
    [HttpStatusCode.PayloadTooLarge, null, 'FILES.ERRORS.TOO_LARGE'],
    [HttpStatusCode.TooManyRequests, null, 'FILES.ERRORS.BUSY'],
    [HttpStatusCode.ServiceUnavailable, null, 'ERRORS.HTTP.SERVER'],
  ])('upload_WithStatus%d_ShowsTheMappedErrorAndKeepsTheDialog', (status, body, expectedKey) => {
    const { store, http, actions } = setup();
    store.addFiles([file('a.pdf', 10)]);

    store.upload();
    http.expectOne(COMPANY_URL).flush(body, { status, statusText: String(status) });

    expect(store.items()[0].status).toBe(UploadStatus.Failed);
    expect(store.items()[0].errorKey).toBe(expectedKey);
    expect(actions.complete).not.toHaveBeenCalled();
  });

  it('close_WithPendingUpload_CancelsTheRequestAndTheAction', () => {
    const { store, http, actions } = setup();
    store.addFiles([file('a.pdf', 10)]);
    store.upload();
    const request = http.expectOne(COMPANY_URL);

    store.close();

    expect(request.cancelled).toBe(true);
    expect(actions.cancel).toHaveBeenCalled();
    expect(actions.complete).not.toHaveBeenCalled();
  });

  it('close_AfterOneStoredFile_CompletesWithSavedSoTheGridReloads', () => {
    const { store, http, actions } = setup();
    store.addFiles([file('a.pdf', 10), file('b.zip', 10)]);
    store.upload();
    http.expectOne(COMPANY_URL).flush({ id: 'new-id' });
    actions.complete.mockClear();

    store.close();

    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
  });

  it('remove_WithKey_DropsThatItem', () => {
    const { store } = setup();
    store.addFiles([file('a.pdf', 1), file('b.pdf', 1)]);

    store.remove(0);

    expect(store.items().map((item) => item.file.name)).toEqual(['b.pdf']);
  });
});
