import { DraftState, type MetricDraftDetail } from '@machines/drafts/draft';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import type { MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import {
  appliedDraftIds,
  buildExamplePayload,
  EMPTY_METRIC_VALUE,
  mapServerErrors,
  type MetricItem,
  netChanges,
  nextKey,
  parseUnitValue,
  stageMetricDrafts,
  toApplyBody,
  unitValue,
} from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

const SAVED: MetricFormValue = {
  key: 'spindle.temperature',
  name: 'Spindle temperature',
  description: null,
  valueKind: MetricValueKind.Numeric,
  unit: 'Temperature|Celsius',
  min: 0,
  max: 100,
  states: [],
};

function savedItem(id: string, value: Partial<MetricFormValue> = {}, isDeleted = false): MetricItem {
  return {
    localId: id,
    savedId: id,
    saved: SAVED,
    value: { ...SAVED, ...value },
    isDeleted,
    draftId: null,
    sourceLocation: null,
  };
}

function newItem(id: string, value: Partial<MetricFormValue> = {}, isDeleted = false): MetricItem {
  return {
    localId: id,
    savedId: null,
    saved: null,
    value: { ...EMPTY_METRIC_VALUE, ...value },
    isDeleted,
    draftId: null,
    sourceLocation: null,
  };
}

describe('netChanges', () => {
  it('netChanges_WithEditBackToSavedValue_HasNoChange', () => {
    const changes = netChanges([savedItem('a', { max: 50 }), savedItem('b', { max: 100 })]);

    expect(changes.updated.map((item) => item.localId)).toEqual(['a']);
  });

  it('netChanges_WithNewItemDeletedAgain_HasNoChange', () => {
    const changes = netChanges([newItem('new-0', { key: 'a.b' }, true)]);

    expect(changes.created).toEqual([]);
  });

  it('netChanges_WithRestoredDelete_HasNoChange', () => {
    const changes = netChanges([savedItem('a', {}, false)]);

    expect(changes.deleted).toEqual([]);
  });

  it('netChanges_WithDeletedSavedItem_ListsItOnceAndNotAsUpdated', () => {
    const changes = netChanges([savedItem('a', { max: 50 }, true)]);

    expect(changes.deleted.map((item) => item.localId)).toEqual(['a']);
    expect(changes.updated).toEqual([]);
  });
});

describe('toApplyBody', () => {
  it('toApplyBody_WithMixedChanges_SendsOnlyChangedItems', () => {
    const items = [
      savedItem('a', { name: 'Renamed ' }),
      savedItem('b'),
      savedItem('c', {}, true),
      newItem('new-0', {
        key: 'door.state',
        name: 'Door',
        valueKind: MetricValueKind.StateSet,
        states: ['open', 'closed'],
      }),
    ];

    const body = toApplyBody(4, netChanges(items));

    expect(body.expectedRevision).toBe(4);
    expect(body.updated.map((item) => item.id)).toEqual(['a']);
    expect(body.updated[0].name).toBe('Renamed');
    expect(body.deleted).toEqual(['c']);
    expect(body.created).toEqual([
      {
        key: 'door.state',
        name: 'Door',
        description: null,
        valueKind: MetricValueKind.StateSet,
        unit: null,
        min: null,
        max: null,
        states: ['open', 'closed'],
      },
    ]);
  });

  it('toApplyBody_WithNewNumericItem_SendsUnitReferenceAndRange', () => {
    const items = [newItem('new-0', { key: 'a.b', name: 'A', unit: 'Length|Metre', min: 1, max: 2 })];

    const body = toApplyBody(0, netChanges(items));

    expect(body.created[0].unit).toEqual({ quantityKind: 'Length', name: 'Metre' });
    expect(body.created[0].min).toBe(1);
    expect(body.created[0].states).toBeNull();
  });
});

describe('mapServerErrors', () => {
  it('mapServerErrors_WithCreatedAndUpdatedPaths_MapsToItems', () => {
    const changes = netChanges([savedItem('a', { max: 50 }), newItem('new-0'), newItem('new-1')]);

    const result = mapServerErrors(
      { 'Created[1].Key': ['bad key'], 'Updated[0].Name': ['bad name'], 'Created[9].Key': ['gone'], Other: ['x'] },
      changes,
    );

    expect(result).toEqual({ 'new-1': ['bad key'], a: ['bad name'] });
  });
});

describe('nextKey', () => {
  it('nextKey_WithUkrainianName_FollowsTheSlug', () => {
    const key = nextKey('', '', 'Температура шпинделя');

    expect(key).toBe('temperatura_shpyndelya');
  });

  it('nextKey_AfterKeyEditedByHand_KeepsTheKey', () => {
    const key = nextKey('Температура шпинделя', 'my.key', 'Температура');

    expect(key).toBe('my.key');
  });

  it('nextKey_WhileKeyEqualsPreviousSlug_FollowsTheName', () => {
    const key = nextKey('Spindle', 'spindle', 'Spindle speed');

    expect(key).toBe('spindle_speed');
  });
});

describe('unit values', () => {
  it('parseUnitValue_AfterUnitValue_RoundTrips', () => {
    const reference = { quantityKind: 'Temperature', name: 'Degree Celsius' };

    expect(parseUnitValue(unitValue(reference))).toEqual(reference);
    expect(parseUnitValue('')).toBeNull();
  });
});

describe('buildExamplePayload', () => {
  const item = (key: string, value: Partial<MetricFormValue>, overrides: Partial<MetricItem> = {}): MetricItem => ({
    localId: key,
    savedId: null,
    saved: null,
    value: { ...SAVED, key, min: null, max: null, states: [], ...value },
    isDeleted: false,
    draftId: null,
    sourceLocation: null,
    ...overrides,
  });

  it('buildExamplePayload_WithEveryKind_UsesPlaceholdersInKeyOrder', () => {
    const payload = buildExamplePayload(
      [
        item('z.mode', { valueKind: MetricValueKind.StateSet, states: ['auto', 'off'] }),
        item('a.door', { valueKind: MetricValueKind.Boolean }),
        item('m.min', { min: -20, max: 200 }),
        item('m.max', { max: 50 }),
        item('m.none', {}),
      ],
      '2026-01-01T00:00:00Z',
    );

    const parsed = JSON.parse(payload ?? '') as {
      sampledAt: string;
      metrics: Record<string, unknown>;
      statuses: unknown[];
    };
    expect(Object.keys(parsed.metrics)).toEqual(['a.door', 'm.max', 'm.min', 'm.none', 'z.mode']);
    expect(parsed.metrics).toEqual({ 'a.door': false, 'm.max': 50, 'm.min': -20, 'm.none': 0, 'z.mode': 'auto' });
    expect(parsed.statuses).toEqual([]);
  });

  it('buildExamplePayload_WithProposedAndNewItems_ShowsThemAndLeavesOutDeletedAndKeylessItems', () => {
    const payload = buildExamplePayload(
      [
        item('saved.key', {}, { savedId: 's1', saved: SAVED }),
        item('proposed.key', { max: 9 }, { draftId: 'draft-1' }),
        item('gone.key', {}, { savedId: 's2', saved: SAVED, isDeleted: true }),
        item('', {}),
      ],
      '2026-01-01T00:00:00Z',
    );

    const parsed = JSON.parse(payload ?? '') as { metrics: Record<string, unknown> };
    expect(parsed.metrics).toEqual({ 'proposed.key': 9, 'saved.key': 0 });
  });

  it('buildExamplePayload_WithANewItemTheFormRejects_LeavesItOutAndKeepsASavedItem', () => {
    const payload = buildExamplePayload(
      [
        item('saved.key', { name: '' }, { savedId: 's1', saved: SAVED }),
        item('new.invalid', { name: '' }),
        item('new.valid', {}),
      ],
      '2026-01-01T00:00:00Z',
    );

    const parsed = JSON.parse(payload ?? '') as { metrics: Record<string, unknown> };
    expect(Object.keys(parsed.metrics)).toEqual(['new.valid', 'saved.key']);
  });

  it.each(
    [
      { name: 'WithoutItems', items: [] },
      {
        name: 'WithOnlyDeletedItems',
        items: [item('gone.key', {}, { savedId: 's2', saved: SAVED, isDeleted: true })],
      },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('buildExamplePayload_%s_ReturnsNull', (_name, { items }) => {
    expect(buildExamplePayload(items, '2026-01-01T00:00:00Z')).toBeNull();
  });
});

describe('stageMetricDrafts', () => {
  const CATALOG = {
    kinds: [{ name: 'Pressure', units: [{ name: 'Bar', abbreviation: 'bar' }] }],
  };

  const DRAFT: MetricDraftDetail = {
    id: 'draft-1',
    state: DraftState.Pending,
    sourceLocation: 'page 1',
    key: 'one.key',
    name: 'Proposed name',
    description: null,
    valueKind: 'numeric',
    quantityKindName: 'pressure',
    unitName: 'BAR',
    min: null,
    max: 6,
    states: null,
  };

  it('stageMetricDrafts_WithKeyOfAnActiveItem_StagesAChangeOfThatItemOnly', () => {
    const items = [savedItem('a', { key: 'one.key' })];

    const staged = stageMetricDrafts(items, 0, [DRAFT], CATALOG);

    expect(staged.items).toHaveLength(1);
    expect(staged.items[0].value).toEqual({ ...SAVED, key: 'one.key', name: 'Proposed name', max: 6 });
    expect(staged.items[0]).toMatchObject({ savedId: 'a', draftId: 'draft-1', sourceLocation: 'page 1' });
    expect(staged.newCount).toBe(0);
  });

  it('stageMetricDrafts_WithNewKey_StagesANewItemWithTheUnitFromTheCatalog', () => {
    const staged = stageMetricDrafts([savedItem('a')], 0, [{ ...DRAFT, key: 'new.key' }], CATALOG);

    expect(staged.items).toHaveLength(2);
    expect(staged.items[1]).toMatchObject({ localId: 'new-0', savedId: null, draftId: 'draft-1' });
    expect(staged.items[1].value).toMatchObject({
      key: 'new.key',
      name: 'Proposed name',
      unit: 'Pressure|Bar',
      max: 6,
    });
    expect(staged.newCount).toBe(1);
  });

  it('stageMetricDrafts_WithUnknownUnitAndMissingName_LeavesThemEmptyForTheUser', () => {
    const draft = { ...DRAFT, key: 'x.key', name: null, quantityKindName: 'Length', unitName: 'Metre' };

    const staged = stageMetricDrafts([], 2, [draft], CATALOG);

    expect(staged.items[0].localId).toBe('new-2');
    expect(staged.items[0].value).toMatchObject({ name: '', unit: '' });
  });

  it('appliedDraftIds_WithStagedAndPlainChanges_ListsOnlyTheStagedDraftsThatAreNotDeleted', () => {
    const staged = stageMetricDrafts([savedItem('a', { key: 'one.key' }), savedItem('b')], 0, [DRAFT], CATALOG);

    const ids = appliedDraftIds(netChanges(staged.items));

    expect(ids).toEqual(['draft-1']);
  });
});
