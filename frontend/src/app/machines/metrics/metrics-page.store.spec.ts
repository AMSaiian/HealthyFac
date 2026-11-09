import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { AuthStore } from '@shared/security/auth.store';
import { MachineState } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import {
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
  states: [],
};

const IMAGE_ID = 'v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';

type Spy = ReturnType<typeof vi.fn>;

interface Setup {
  store: InstanceType<typeof MetricsPageStore>;
  api: { getDefinitions: Spy; apply: Spy; getSchema: Spy };
  resolveLinks: Spy;
  messages: { add: Spy };
  confirmation: { confirm: Spy };
}

async function setup(definitions: MetricDefinitionsResponse = LOADED, apply?: Spy): Promise<Setup> {
  const messages = { add: vi.fn() };
  const confirmation = { confirm: vi.fn((options: { accept: () => void }) => options.accept()) };
  const resolveLinks = vi.fn(() => of(new Map([[IMAGE_ID, 'https://api/blob?sig=x']])));
  const api = {
    getDefinitions: vi.fn(() => of(definitions)),
    apply: apply ?? vi.fn(() => of({ revision: 4 })),
    getSchema: vi.fn(() => of({ revision: 3, schema: {} })),
  };
  TestBed.configureTestingModule({
    providers: [
      MetricsPageStore,
      { provide: MetricDefinitionsApi, useValue: api },
      { provide: MetricUnitsApi, useValue: { getUnits: () => of({ kinds: [] }) } },
      { provide: MachinesApi, useValue: { get: () => of(MACHINE) } },
      { provide: RichTextImageLinks, useValue: { resolve: resolveLinks } },
      { provide: MessageService, useValue: messages },
      { provide: ConfirmationService, useValue: confirmation },
      { provide: AuthStore, useValue: { hasPermission: () => true } },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const store = TestBed.inject(MetricsPageStore);
  store.openMachine(MACHINE.id).subscribe();
  TestBed.tick();
  await TestBed.inject(ApplicationRef).whenStable();
  return { store, api, messages, confirmation, resolveLinks };
}

function stageNew(store: Setup['store']): void {
  store.startAdd();
  store.setDraft(NEW_VALUE);
  store.setDraftValidity(true);
  store.done();
}

describe('MetricsPageStore', () => {
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
    store.setDraft(store.editInitial()!);
    store.setDraftValidity(true);

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
    store.setDraft({ ...store.editInitial()!, name: 'Changed' });

    store.select('b');

    expect(confirmation.confirm).toHaveBeenCalledTimes(1);
    expect(store.selectedId()).toBe('a');
  });

  it('select_OfPreviouslySelectedItemWhileAdding_AsksAndShowsItOnAccept', async () => {
    const { store, confirmation } = await setup();
    store.startAdd();
    store.setDraft(NEW_VALUE);
    store.setDraftValidity(true);

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
    store.setDraft(store.editInitial()!);

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
});
