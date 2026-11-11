import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';

import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';

import { FileTieStore } from './file-tie.store';

const MACHINE_ID = 'encrypted-machine-id';
const ITEM = { id: 'file-id', fileName: 'manual', rowVersion: 9 };

describe('FileTieStore', () => {
  function setup(): {
    store: InstanceType<typeof FileTieStore>;
    http: HttpTestingController;
    actions: { complete: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    messages: { add: ReturnType<typeof vi.fn> };
  } {
    const actions = { complete: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        FileTieStore,
        FilesApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(FileTieStore);
    store.start({ type: FileActionType.Tie, payload: { item: ITEM } });
    return { store, http: TestBed.inject(HttpTestingController), actions, messages };
  }

  it('saveDisabledReason_WithoutAMachine_AsksToPickOne', () => {
    const { store } = setup();

    expect(store.saveDisabledReason()).toBe('FILES.TIE_DIALOG.NO_MACHINE');

    store.pick(MACHINE_ID);

    expect(store.saveDisabledReason()).toBeNull();
  });

  it('save_WithAMachine_PutsTheMachineAndTheRowVersionAndClosesWithSaved', () => {
    const { store, http, actions } = setup();
    store.pick(MACHINE_ID);

    store.save();
    const request = http.expectOne('/api/document/v1/files/file-id/machine');
    request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ machineId: MACHINE_ID, rowVersion: 9 });
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
  });

  it('save_WithConflict_ShowsTheConflictAndClosesWithSavedSoTheGridReloads', () => {
    const { store, http, actions, messages } = setup();
    store.pick(MACHINE_ID);

    store.save();
    http
      .expectOne('/api/document/v1/files/file-id/machine')
      .flush(null, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'FILES.ERRORS.CONFLICT' }));
    expect(actions.complete).toHaveBeenCalledWith(FileActionType.Saved);
  });
});
