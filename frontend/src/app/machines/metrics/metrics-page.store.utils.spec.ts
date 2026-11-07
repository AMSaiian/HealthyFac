import { type MetricDefinition, MetricValueKind } from '@machines/metrics/metric-definition';
import type { MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';
import {
  buildExamplePayload,
  EMPTY_METRIC_VALUE,
  mapServerErrors,
  type MetricItem,
  netChanges,
  nextKey,
  parseUnitValue,
  toApplyBody,
  unitValue,
} from '@machines/metrics/metrics-page.store.utils';

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
  return { localId: id, savedId: id, saved: SAVED, value: { ...SAVED, ...value }, isDeleted };
}

function newItem(id: string, value: Partial<MetricFormValue> = {}, isDeleted = false): MetricItem {
  return { localId: id, savedId: null, saved: null, value: { ...EMPTY_METRIC_VALUE, ...value }, isDeleted };
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
  const definition = (key: string, overrides: Partial<MetricDefinition>): MetricDefinition => ({
    id: key,
    key,
    name: key,
    description: null,
    valueKind: MetricValueKind.Numeric,
    unit: { quantityKind: 'Temperature', name: 'DegreeCelsius', abbreviation: 'C' },
    min: null,
    max: null,
    states: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...overrides,
  });

  it('buildExamplePayload_WithEveryKind_UsesPlaceholdersInKeyOrder', () => {
    const payload = buildExamplePayload(
      [
        definition('z.mode', { valueKind: MetricValueKind.StateSet, states: ['auto', 'off'] }),
        definition('a.door', { valueKind: MetricValueKind.Boolean }),
        definition('m.min', { min: -20, max: 200 }),
        definition('m.max', { max: 50 }),
        definition('m.none', {}),
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

  it('buildExamplePayload_WithoutMetrics_ReturnsNull', () => {
    expect(buildExamplePayload([], '2026-01-01T00:00:00Z')).toBeNull();
  });
});
