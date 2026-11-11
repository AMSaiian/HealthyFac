import { ApplicationRef } from '@angular/core';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';

import { CompareField } from '@machines/drafts/compare/draft-compare.store.utils';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachineActionType } from '@machines/machine-actions';

import { DraftCompareStore } from './draft-compare.store';

const DRAFT_URL = '/api/core/v1/machine-drafts/draft-1';
const RESPONSE = {
  draft: {
    id: 'draft-1',
    state: 'pending',
    isCreate: false,
    attemptId: 'a1',
    fileId: 'f1',
    name: 'Pump X',
    serialNumber: null,
    description: null,
    rowVersion: 1,
  },
  current: { machineId: 'm1', name: 'Pump', serialNumber: 'SN-1', description: '<p>old</p>', rowVersion: 7 },
};

describe('DraftCompareStore', () => {
  function setup(): {
    store: InstanceType<typeof DraftCompareStore>;
    http: HttpTestingController;
    actions: { complete: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    messages: { add: ReturnType<typeof vi.fn> };
  } {
    const actions = { complete: vi.fn(), cancel: vi.fn() };
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        DraftCompareStore,
        DraftsApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: actions },
        { provide: MessageService, useValue: messages },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(DraftCompareStore);
    store.start({ type: DraftActionType.CompareMachine, payload: { draftId: 'draft-1' } });
    return { store, http: TestBed.inject(HttpTestingController), actions, messages };
  }

  async function load(http: HttpTestingController): Promise<void> {
    TestBed.tick();
    http.expectOne(DRAFT_URL).flush(RESPONSE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  it('saveDisabledReason_WithNothingTaken_AsksToTakeAChange', async () => {
    const { store, http } = setup();
    await load(http);

    expect(store.saveDisabledReason()).toBe('DRAFTS.COMPARE.NO_CHANGES');

    store.take(CompareField.Name, true);

    expect(store.saveDisabledReason()).toBeNull();
  });

  it('take_WithNullProposal_DoesNotMakeTheFieldTaken', async () => {
    const { store, http } = setup();
    await load(http);

    store.take(CompareField.SerialNumber, true);

    expect(store.rows().find((row) => row.field === CompareField.SerialNumber)?.isTaken).toBe(false);
    expect(store.saveDisabledReason()).toBe('DRAFTS.COMPARE.NO_CHANGES');
  });

  it('save_WithNameTakenAndEdited_SendsTheMergedValuesAndTheRowVersion', async () => {
    const { store, http, actions } = setup();
    await load(http);
    store.take(CompareField.Name, true);
    store.edit(CompareField.Name, 'Pump Y');

    store.save();
    const request = http.expectOne('/api/core/v1/machine-drafts/draft-1/apply');
    request.flush({ machineId: 'm1' });

    expect(request.request.body).toEqual({
      name: 'Pump Y',
      serialNumber: 'SN-1',
      description: '<p>old</p>',
      rowVersion: 7,
    });
    expect(actions.complete).toHaveBeenCalledTimes(1);
    expect(actions.complete).toHaveBeenCalledWith(MachineActionType.Saved);
  });

  it('save_WithEmptyTakenName_StaysDisabledBecauseTheFormIsInvalid', async () => {
    const { store, http } = setup();
    await load(http);
    store.take(CompareField.Name, true);
    store.edit(CompareField.Name, '  ');

    expect(store.saveDisabledReason()).toBe('DRAFTS.COMPARE.INVALID');
  });

  it('save_WithConflict_ShowsTheToastAndLoadsTheCompareDataAgain', async () => {
    const { store, http, messages } = setup();
    await load(http);
    store.take(CompareField.Name, true);

    store.save();
    http
      .expectOne('/api/core/v1/machine-drafts/draft-1/apply')
      .flush(null, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });
    TestBed.tick();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'DRAFTS.COMPARE.CONFLICT' }));
    http.expectOne(DRAFT_URL).flush(RESPONSE);
    expect(store.saveDisabledReason()).toBe('DRAFTS.COMPARE.NO_CHANGES');
  });
});
