import { EMPTY_CELL } from '@shared/grid/table/grid-display';

import type { MetricDefinition } from '@machines/metrics/metric-definition';
import { ConditionSourceKind } from '@machines/phases/operating-phase';
import type { ConditionSourceOption } from '@machines/phases/phase-form/phase-form.utils';

import { metricName, numberText, sourceLabel, statesText } from './phase-details.utils';

const OPTIONS: readonly ConditionSourceOption[] = [
  { id: 'status-1', kind: ConditionSourceKind.Status, label: 'E-101' },
  { id: 'metric-1', kind: ConditionSourceKind.Numeric, label: 'Temperature' },
];

const METRICS = [{ id: 'metric-1', name: 'Temperature' }] as unknown as readonly MetricDefinition[];

describe('sourceLabel', { tags: ['SignalModel'] }, () => {
  it.each([
    ['KnownId_ReturnsTheOptionLabel', 'status-1', 'E-101'],
    ['UnknownId_ReturnsEmptyCell', 'missing', EMPTY_CELL],
  ])('sourceLabel_With%s', (_name, id, expected) => {
    expect(sourceLabel(id, OPTIONS)).toBe(expected);
  });
});

describe('metricName', { tags: ['SignalModel'] }, () => {
  it.each([
    ['KnownId_ReturnsTheMetricName', 'metric-1', 'Temperature'],
    ['UnknownId_ReturnsEmptyCell', 'missing', EMPTY_CELL],
  ])('metricName_With%s', (_name, id, expected) => {
    expect(metricName(id, METRICS)).toBe(expected);
  });
});

describe('numberText', { tags: ['SignalModel'] }, () => {
  it.each([
    ['Null_ReturnsEmptyCell', null, EMPTY_CELL],
    [
      'SevenFractionDigits_RoundsToSix',
      1.2345678,
      new Intl.NumberFormat(undefined, { maximumFractionDigits: 6 }).format(1.234568),
    ],
    ['Zero_ShowsZero', 0, '0'],
  ])('numberText_With%s', (_name, value, expected) => {
    expect(numberText(value)).toBe(expected);
  });
});

describe('statesText', { tags: ['SignalModel'] }, () => {
  it('statesText_WithStates_JoinsThemWithCommas', () => {
    expect(statesText(['Idle', 'Run'])).toBe('Idle, Run');
  });

  it('statesText_WithNoStates_ReturnsEmptyCell', () => {
    expect(statesText([])).toBe(EMPTY_CELL);
    expect(statesText(null)).toBe(EMPTY_CELL);
  });
});
