import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, Subject, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { HistoryStore } from '@shared/history/history.store';
import { MACHINES_RETURN_TARGET } from '@machines/machines-return-target';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { MachineState } from '@machines/machine';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MachinesApi } from '@machines/machines-api';
import {
  MetricBehavior,
  type MetricDefinition,
  type MetricDefinitionsResponse,
  MetricValueKind,
} from '@machines/metrics/metric-definition';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import type { MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';

import { MetricsPageStore } from './metrics-page.store';

const MACHINE = {
  id: 'machine-1',
  name: 'Lathe',
  serialNumber: 'S1',
  gatewayId: 'abcdefghijklmnop',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
  pendingDraftCount: 0,
};

function definition(id: string, key: string): MetricDefinition {
  return {
    id,
    key,
    name: `Name ${id}`,
    description: null,
    valueKind: MetricValueKind.Numeric,
    unit: { quantityKind: 'Temperature', name: 'Celsius', abbreviation: 'C' },
    min: 0,
    max: 100,
    states: null,
    warningLow: null,
    warningHigh: null,
    alarmLow: null,
    alarmHigh: null,
    behavior: MetricBehavior.Level,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };
}

const LOADED: MetricDefinitionsResponse = {
  revision: 3,
  definitions: [definition('a', 'one.key'), definition('b', 'two.key')],
};

const NEW_VALUE: MetricFormValue = {
  key: 'new.key',
  name: 'New metric',
  description: null,
  valueKind: MetricValueKind.Boolean,
  unit: '',
  min: null,
  max: null,
  warningLow: null,
  warningHigh: null,
  alarmLow: null,
  alarmHigh: null,
  behavior: MetricBehavior.Level,
  states: [],
};

const DRAFTS = {
  groups: [
    {
      machineId: 'machine-1',
      machineName: 'Lathe',
      machineDraft: null,
      metrics: [
        {
          id: 'draft-key',
          state: 'pending',
          sourceLocation: 'page 1',
          key: 'one.key',
          name: 'Proposed one',
          description: null,
          valueKind: 'numeric',
          quantityKindName: null,
          unitName: null,
          min: null,
          max: 50,
          states: null,
        },
        {
          id: 'draft-new',
          state: 'pending',
          sourceLocation: 'page 2',
          key: 'fresh.key',
          name: 'Fresh',
          description: null,
          valueKind: 'boolean',
          quantityKindName: null,
          unitName: null,
          min: null,
          max: null,
          states: null,
        },
        {
          id: 'draft-old',
          state: 'applied',
          sourceLocation: null,
          key: 'old.key',
          name: 'Old',
          description: null,
          valueKind: 'boolean',
          quantityKindName: null,
          unitName: null,
          min: null,
          max: null,
          states: null,
        },
      ],
      statuses: [],
    },
    { machineId: 'machine-2', machineName: 'Press', machineDraft: null, metrics: [], statuses: [] },
  ],
};

const IMAGE_ID = 'v1_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MetricsPageStore>;
  api: { getDefinitions: Spy; apply: Spy; getSchema: Spy };
  draftsApi: { listForScope: Spy };
  actions: { complete: Spy };
  history: { canGoBack: WritableSignal<boolean>; back: Spy };
  resolveLinks: Spy;
  messages: { add: Spy };
  confirmation: { confirm: Spy };
}

async function setup(
  definitions: MetricDefinitionsResponse = LOADED,
  apply?: Spy,
  overrides: { getDefinitions?: Spy; listForScope?: Spy; isHistoryReal?: boolean } = {},
): Promise<Setup> {
  const messages = { add: vi.fn() };
  const confirmation = { confirm: vi.fn((options: { accept: () => void }) => options.accept()) };
  const resolveLinks = vi.fn(() => of(new Map([[IMAGE_ID, 'https://api/blob?sig=x']])));
  const api = {
    getDefinitions: overrides.getDefinitions ?? vi.fn(() => of(definitions)),
    apply: apply ?? vi.fn(() => of({ revision: 4 })),
    getSchema: vi.fn(() => of({ revision: 3, schema: {} })),
  };
  const draftsApi = {
    listForScope: overrides.listForScope ?? vi.fn(() => of(DRAFTS)),
  };
  const actions = { complete: vi.fn() };
  const history = { canGoBack: signal(false), back: vi.fn(() => Promise.resolve()) };
  TestBed.configureTestingModule({
    providers: [
      MetricsPageStore,
      { provide: DraftsApi, useValue: draftsApi },
      { provide: ActionStore, useValue: actions },
      ...(overrides.isHistoryReal === true ? [provideRouter([])] : [{ provide: HistoryStore, useValue: history }]),
      { provide: MetricDefinitionsApi, useValue: api },
      { provide: MetricUnitsApi, useValue: { getUnits: () => of({ kinds: [] }) } },
      { provide: MachinesApi, useValue: { get: () => of(MACHINE) } },
      { provide: RichTextImageLinks, useValue: { resolve: resolveLinks } },
      { provide: MessageService, useValue: messages },
      { provide: ConfirmationService, useValue: confirmation },
      provideFakeAuthStore({ hasPermission: () => true }),
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const store = TestBed.inject(MetricsPageStore);
  store.openMachine(MACHINE.id).subscribe();
  TestBed.tick();
  await TestBed.inject(ApplicationRef).whenStable();
  return { store, api, draftsApi, actions, history, messages, confirmation, resolveLinks };
}

function stageNew(store: Setup['store']): void {
  store.startAdd();
  store.setFormValue(NEW_VALUE);
  store.setFormValidity(true);
  store.done();
}

describe('MetricsPageStore', { tags: ['SignalModel'] }, () => {
  it('descriptionLinks_WithImageInTheSelectedDescription_ResolvesLinksOnceForThatDefinition', async () => {
    const withImage: MetricDefinitionsResponse = {
      revision: 3,
      definitions: [
        { ...definition('a', 'one.key'), description: `<p><img data-file-id="${IMAGE_ID}" alt="x"></p>` },
        definition('b', 'two.key'),
      ],
    };

    const { store, resolveLinks } = await setup(withImage);

    expect(resolveLinks).toHaveBeenCalledTimes(1);
    expect(resolveLinks).toHaveBeenCalledWith([IMAGE_ID]);
    expect(store.descriptionLinks().get(IMAGE_ID)).toBe('https://api/blob?sig=x');
  });

  it('descriptionLinks_WithoutImages_AsksForNoLinks', async () => {
    const { store, resolveLinks } = await setup();

    expect(resolveLinks).not.toHaveBeenCalled();
    expect(store.descriptionLinks().size).toBe(0);
  });

  it('load_WithDefinitions_SelectsTheFirstItem', async () => {
    const { store } = await setup();

    expect(store.selectedId()).toBe('a');
    expect(store.hasChanges()).toBe(false);
  });

  it('load_WithNoDefinitions_SelectsNothing', async () => {
    const { store } = await setup({ revision: 0, definitions: [] });

    expect(store.selectedId()).toBeNull();
    expect(store.items()).toEqual([]);
  });

  it('done_WithUnchangedDraft_StagesNothing', async () => {
    const { store } = await setup();
    store.startEdit();
    store.setFormValue(store.editInitial()!);
    store.setFormValidity(true);

    store.done();

    expect(store.edit()).not.toBeNull();
    expect(store.hasChanges()).toBe(false);
  });

  it('done_WithNewMetric_StagesItAndSelectsIt', async () => {
    const { store } = await setup();

    stageNew(store);

    expect(store.hasChanges()).toBe(true);
    expect(store.selectedId()).toBe('new-0');
    expect(store.edit()).toBeNull();
  });

  it('remove_OfNewItem_LeavesNoChange', async () => {
    const { store } = await setup();
    stageNew(store);

    store.remove('new-0');

    expect(store.hasChanges()).toBe(false);
    expect(store.selectedId()).toBe('a');
  });

  it('restore_AfterRemoveOfSavedItem_LeavesNoChange', async () => {
    const { store } = await setup();

    store.remove('a');
    expect(store.hasChanges()).toBe(true);
    store.restore('a');

    expect(store.hasChanges()).toBe(false);
  });

  it('select_WithChangedOpenForm_AsksAndKeepsSelectionOnReject', async () => {
    const { store, confirmation } = await setup();
    confirmation.confirm.mockImplementation(() => undefined);
    store.startEdit();
    store.setFormValue({ ...store.editInitial()!, name: 'Changed' });

    store.select('b');

    expect(confirmation.confirm).toHaveBeenCalledTimes(1);
    expect(store.selectedId()).toBe('a');
  });

  it('select_OfPreviouslySelectedItemWhileAdding_AsksAndShowsItOnAccept', async () => {
    const { store, confirmation } = await setup();
    store.startAdd();
    store.setFormValue(NEW_VALUE);
    store.setFormValidity(true);

    store.select('a');

    expect(confirmation.confirm).toHaveBeenCalledTimes(1);
    expect(store.selectedId()).toBe('a');
    expect(store.edit()).toBeNull();
  });

  it('cancelEdit_OfAdd_LeavesNothingSelected', async () => {
    const { store } = await setup();
    store.startAdd();

    store.cancelEdit();

    expect(store.selectedId()).toBeNull();
    expect(store.edit()).toBeNull();
  });

  it('select_WithUnchangedOpenForm_SwitchesWithoutAsking', async () => {
    const { store, confirmation } = await setup();
    store.startEdit();
    store.setFormValue(store.editInitial()!);

    store.select('b');

    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(store.selectedId()).toBe('b');
    expect(store.edit()).toBeNull();
  });

  it('apply_WithStagedChanges_SendsOnlyNetChangesAndReloads', async () => {
    const { store, api, messages } = await setup();
    stageNew(store);
    store.remove('b');

    store.apply();
    TestBed.tick();

    const body = api.apply.mock.calls[0][1];
    expect(body.expectedRevision).toBe(3);
    expect(body.created).toHaveLength(1);
    expect(body.updated).toEqual([]);
    expect(body.deleted).toEqual(['b']);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    expect(api.getDefinitions).toHaveBeenCalledTimes(2);
  });

  it('apply_WithConflict_ShowsMessageAndKeepsStagedChanges', async () => {
    const apply = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict, statusText: 'Conflict' })),
    );
    const { store, messages } = await setup(LOADED, apply);
    stageNew(store);

    store.apply();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'METRICS.TOASTS.CONFLICT' }));
    expect(store.hasChanges()).toBe(true);
    expect(store.isApplying()).toBe(false);
  });

  it('apply_WithBadRequest_MarksTheFailedItem', async () => {
    const apply = vi.fn(() =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: HttpStatusCode.BadRequest,
            statusText: 'Bad Request',
            error: { errors: { 'Created[0].Key': ['The key is invalid.'] } },
          }),
      ),
    );
    const { store } = await setup(LOADED, apply);
    stageNew(store);

    store.apply();

    expect(store.itemErrors()).toEqual({ 'new-0': ['The key is invalid.'] });
    expect(store.hasChanges()).toBe(true);
  });

  it('apply_WithOpenForm_SendsNothing', async () => {
    const { store, api } = await setup();
    stageNew(store);
    store.startEdit();

    store.apply();

    expect(api.apply).not.toHaveBeenCalled();
  });

  it.each<[string, (store: Setup['store']) => Promise<void> | void, boolean]>([
    ['ABlankNewForm', (store) => store.startAdd(), true],
    [
      'AProposedDraftItem',
      async (store) => {
        store.loadDrafts('attempt-1');
        TestBed.tick();
        await TestBed.inject(ApplicationRef).whenStable();
        TestBed.tick();
        store.select('new-0');
        store.startEdit();
      },
      true,
    ],
    [
      'ASavedMetric',
      (store) => {
        store.select('a');
        store.startEdit();
      },
      false,
    ],
    ['NoOpenForm', () => undefined, false],
  ])('isEditedItemUnsaved_With%s_ReturnsTheExpectedFlag', async (_name, open, expected) => {
    const { store } = await setup();
    await open(store);

    expect(store.isEditedItemUnsaved()).toBe(expected);
  });

  it('loadDrafts_WithAChat_ReadsTheChatDraftsAndStagesThem', async () => {
    const { store, draftsApi } = await setup();

    store.loadDrafts(null, 'chat-1');
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    TestBed.tick();

    expect(draftsApi.listForScope).toHaveBeenCalledWith({ kind: 'chat', id: 'chat-1' }, expect.anything());
    expect(store.items().map((item) => item.draftId)).toContain('draft-key');
  });

  it('loadDrafts_WithAttempt_StagesTheChangeAndTheNewItemOfThatMachineOnly', async () => {
    const { store, draftsApi } = await setup();

    store.loadDrafts('attempt-1');
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    TestBed.tick();

    expect(draftsApi.listForScope).toHaveBeenCalledWith({ kind: 'attempt', id: 'attempt-1' }, expect.anything());
    expect(store.items().map((item) => [item.localId, item.draftId])).toEqual([
      ['a', 'draft-key'],
      ['b', null],
      ['new-0', 'draft-new'],
    ]);
    expect(store.items()[0].value).toMatchObject({ name: 'Proposed one', max: 50 });
    expect(store.hasChanges()).toBe(true);
  });

  it('apply_WithStagedDrafts_SendsTheirDraftIdsAndCompletesChanged', async () => {
    const { store, api, actions } = await setup();
    store.loadDrafts('attempt-1');
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    TestBed.tick();

    store.apply();

    const body = api.apply.mock.calls[0][1] as {
      created: { draftId: string | null }[];
      updated: { draftId: string | null }[];
    };

    expect([...body.created, ...body.updated].map((item) => item.draftId)).toEqual(['draft-new', 'draft-key']);
    expect(actions.complete).toHaveBeenCalledWith(DraftActionType.Changed);
  });

  it('apply_WhenTheDefinitionsReloadBeforeTheDrafts_DoesNotStageTheAppliedDraftsAgain', async () => {
    const reloadedDefinitions = new Subject<MetricDefinitionsResponse>();
    const reloadedDrafts = new Subject<typeof DRAFTS>();
    const getDefinitions = vi.fn().mockReturnValueOnce(of(LOADED)).mockReturnValue(reloadedDefinitions);
    const listForScope = vi.fn().mockReturnValueOnce(of(DRAFTS)).mockReturnValue(reloadedDrafts);
    const { store } = await setup(LOADED, undefined, { getDefinitions, listForScope });
    store.loadDrafts('attempt-1');
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    TestBed.tick();

    store.apply();
    TestBed.tick();
    reloadedDefinitions.next({ ...LOADED, revision: 4 });
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();

    expect(store.items().map((item) => item.draftId)).toEqual([null, null]);

    reloadedDrafts.next({
      groups: DRAFTS.groups.map((group) => ({
        ...group,
        metrics: group.metrics.map((metric) => ({ ...metric, state: 'applied' })),
      })),
    });
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();

    expect(store.items().map((item) => item.draftId)).toEqual([null, null]);
    expect(store.hasChanges()).toBe(false);
  });

  it('back_WithoutPreviousEntry_GoesToMachines', async () => {
    const { store } = await setup(LOADED, undefined, { isHistoryReal: true });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    store.back();

    expect(navigate).toHaveBeenCalledWith('/machines');
  });

  it('back_WithPreviousEntry_AsksTheHistoryForTheMachinesTarget', async () => {
    const { store, history } = await setup();
    history.canGoBack.set(true);

    store.back();

    expect(history.back).toHaveBeenCalledWith(MACHINES_RETURN_TARGET);
  });

  it('apply_WithPreviousEntry_GoesBackWithoutTheLeaveQuestion', async () => {
    const { store, history, confirmation } = await setup(LOADED);
    history.canGoBack.set(true);
    let isLeavingAtBack = false;
    history.back.mockImplementation(() => {
      isLeavingAtBack = store.isLeaving();
      return Promise.resolve();
    });
    stageNew(store);

    store.apply();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(history.back).toHaveBeenCalledWith(MACHINES_RETURN_TARGET);
    expect(isLeavingAtBack).toBe(true);
    expect(store.isLeaving()).toBe(false);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('apply_WithoutPreviousEntry_StaysOnThePage', async () => {
    const { store, history } = await setup(LOADED);
    stageNew(store);

    store.apply();
    TestBed.tick();

    expect(history.back).not.toHaveBeenCalled();
  });

  it('apply_WithPreviousEntryAndAFailure_StaysOnThePage', async () => {
    const apply = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict, statusText: 'Conflict' })),
    );
    const { store, history } = await setup(LOADED, apply);
    history.canGoBack.set(true);
    stageNew(store);

    store.apply();
    TestBed.tick();

    expect(history.back).not.toHaveBeenCalled();
  });

  it('confirmedDiscard_WithPreviousEntry_GoesBackWithoutTheLeaveQuestion', async () => {
    const { store, history, confirmation } = await setup(LOADED);
    history.canGoBack.set(true);
    stageNew(store);

    store.requestDiscard();

    expect(confirmation.confirm).toHaveBeenCalledTimes(1);
    expect(history.back).toHaveBeenCalledWith(MACHINES_RETURN_TARGET);
  });

  it('requestDiscard_WithoutPreviousEntry_StaysOnThePage', async () => {
    const { store, history } = await setup();
    stageNew(store);

    store.requestDiscard();

    expect(history.back).not.toHaveBeenCalled();
  });
});
