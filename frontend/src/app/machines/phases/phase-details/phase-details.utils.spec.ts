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

describe('sourceLabel', () => {
  it('sourceLabel_WithKnownId_ReturnsTheOptionLabel', () => {
    expect(sourceLabel('status-1', OPTIONS)).toBe('E-101');
  });

  it('sourceLabel_WithUnknownId_ReturnsEmptyCell', () => {
    expect(sourceLabel('missing', OPTIONS)).toBe(EMPTY_CELL);
  });
});

describe('metricName', () => {
  it('metricName_WithKnownId_ReturnsTheMetricName', () => {
    expect(metricName('metric-1', METRICS)).toBe('Temperature');
  });

  it('metricName_WithUnknownId_ReturnsEmptyCell', () => {
    expect(metricName('missing', METRICS)).toBe(EMPTY_CELL);
  });
});

describe('numberText', () => {
  it('numberText_WithNull_ReturnsEmptyCell', () => {
    expect(numberText(null)).toBe(EMPTY_CELL);
  });

  it('numberText_WithSevenFractionDigits_RoundsToSix', () => {
    expect(numberText(1.2345678)).toBe(new Intl.NumberFormat(undefined, { maximumFractionDigits: 6 }).format(1.234568));
  });

  it('numberText_WithZero_ShowsZero', () => {
    expect(numberText(0)).toBe('0');
  });
});

describe('statesText', () => {
  it('statesText_WithStates_JoinsThemWithCommas', () => {
    expect(statesText(['Idle', 'Run'])).toBe('Idle, Run');
  });

  it('statesText_WithNoStates_ReturnsEmptyCell', () => {
    expect(statesText([])).toBe(EMPTY_CELL);
    expect(statesText(null)).toBe(EMPTY_CELL);
  });
});
