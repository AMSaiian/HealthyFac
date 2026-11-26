import { MetricBehavior, type MetricDefinition, MetricValueKind } from '@machines/metrics/metric-definition';
import { ConditionSourceKind, type OperatingPhase, PhaseConditionOperator } from '@machines/phases/operating-phase';
import type { PhaseConditionFormValue } from '@machines/phases/phase-form/phase-form-schema';
import {
  type ConditionSourceOption,
  EMPTY_FORM_VALUE,
  hasNoLimitValues,
  limitMetricOptions,
  maskedMetricNames,
  newCondition,
  newLimit,
  nextPriority,
  normalizedCondition,
  OPERATOR_LABEL_KEYS,
  operatorSign,
  toBody,
  toFormValue,
} from '@machines/phases/phase-form/phase-form.utils';

const OPTIONS: readonly ConditionSourceOption[] = [
  { id: 's1', kind: ConditionSourceKind.Status, label: 'RUN' },
  { id: 'm1', kind: ConditionSourceKind.Numeric, label: 'Temperature' },
  { id: 'm2', kind: ConditionSourceKind.StateSet, label: 'Mode' },
];

function metric(id: string, valueKind: MetricValueKind, extra: Partial<MetricDefinition> = {}): MetricDefinition {
  return {
    id,
    key: id,
    name: id,
    description: null,
    valueKind,
    unit: { quantityKind: 'q', name: 'n', abbreviation: 'u' },
    min: null,
    max: null,
    states: null,
    warningLow: null,
    warningHigh: null,
    alarmLow: null,
    alarmHigh: null,
    behavior: MetricBehavior.Level,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...extra,
  };
}

function condition(sourceId: string, operator: PhaseConditionOperator): PhaseConditionFormValue {
  return { sourceId, operator, value: 5, upperValue: 9, states: ['a'] };
}

describe('phase form utils', { tags: ['SignalModel'] }, () => {
  it('normalizedCondition_OperatorOfAnotherKind_ChangesToTheFirstOperatorAndClearsTheValues', () => {
    const result = normalizedCondition(condition('s1', PhaseConditionOperator.GreaterThan), OPTIONS);

    expect(result).toEqual({
      sourceId: 's1',
      operator: PhaseConditionOperator.Active,
      value: null,
      upperValue: null,
      states: [],
    });
  });

  it('normalizedCondition_OperatorOfTheKind_ReturnsTheSameObject', () => {
    const input = condition('m1', PhaseConditionOperator.Between);

    expect(normalizedCondition(input, OPTIONS)).toBe(input);
  });

  it('normalizedCondition_UnknownSource_ReturnsTheSameObject', () => {
    const input = condition('gone', PhaseConditionOperator.In);

    expect(normalizedCondition(input, OPTIONS)).toBe(input);
  });

  it('newCondition_WithoutSource_StartsAsAStatusCondition', () => {
    expect(newCondition(null).operator).toBe(PhaseConditionOperator.Active);
  });

  it('limitMetricOptions_ConditionReadsAMetric_LeavesOutThatMetricAndNonNumericOnes', () => {
    const metrics = [
      metric('m1', MetricValueKind.Numeric),
      metric('m3', MetricValueKind.Numeric),
      metric('m2', MetricValueKind.StateSet),
    ];

    const result = limitMetricOptions(metrics, [condition('m1', PhaseConditionOperator.GreaterThan)]);

    expect(result.map((item) => item.id)).toEqual(['m3']);
  });

  it('newLimit_MetricWithoutDefaultLimits_SetsNoLimit', () => {
    expect(newLimit(metric('m1', MetricValueKind.Numeric))).toEqual({
      metricDefinitionId: 'm1',
      warningLow: null,
      warningHigh: null,
      alarmLow: null,
      alarmHigh: null,
    });
  });

  it('newLimit_MetricWithDefaultLimits_CopiesThem', () => {
    const result = newLimit(metric('m1', MetricValueKind.Numeric, { warningHigh: 80, alarmHigh: 90 }));

    expect(result).toMatchObject({ warningHigh: 80, alarmHigh: 90, warningLow: null });
  });

  it('maskedMetricNames_StatusAndMetricConditions_NamesOnlyTheMetrics', () => {
    const metrics = [metric('m1', MetricValueKind.Numeric, { name: 'Temperature' })];

    const result = maskedMetricNames(
      [condition('s1', PhaseConditionOperator.Active), condition('m1', PhaseConditionOperator.GreaterThan)],
      metrics,
    );

    expect(result).toEqual(['Temperature']);
  });

  it('nextPriority_NoPhases_ReturnsZero', () => {
    expect(nextPriority([])).toBe(0);
  });

  it('nextPriority_StoredPhases_ReturnsOneAboveTheHighest', () => {
    const phases = [{ priority: 2 }, { priority: 7 }, { priority: 4 }] as OperatingPhase[];

    expect(nextPriority(phases)).toBe(8);
  });

  it('toBody_ConditionsOfEachKind_SplitsTheSourceAndDropsValuesThatTheOperatorIgnores', () => {
    const result = toBody(
      {
        ...EMPTY_FORM_VALUE,
        code: ' idle ',
        name: ' Idle ',
        description: '  ',
        conditions: [
          condition('s1', PhaseConditionOperator.Active),
          condition('m1', PhaseConditionOperator.GreaterThan),
          condition('m2', PhaseConditionOperator.In),
        ],
      },
      3,
      new Set(['s1']),
    );

    expect(result).toMatchObject({ code: 'idle', name: 'Idle', description: null, priority: 3 });
    expect(result.conditions).toEqual([
      {
        metricDefinitionId: null,
        statusDefinitionId: 's1',
        operator: 'active',
        value: 5,
        upperValue: null,
        states: null,
      },
      {
        metricDefinitionId: 'm1',
        statusDefinitionId: null,
        operator: 'greaterThan',
        value: 5,
        upperValue: null,
        states: null,
      },
      { metricDefinitionId: 'm2', statusDefinitionId: null, operator: 'in', value: 5, upperValue: null, states: ['a'] },
    ]);
  });

  it('toFormValue_PhaseWithStatusAndMetricConditions_ReadsTheSourceId', () => {
    const phase: OperatingPhase = {
      id: 'p',
      code: 'c',
      name: 'n',
      description: null,
      priority: 1,
      isTracked: false,
      conditions: [
        {
          metricDefinitionId: null,
          statusDefinitionId: 's1',
          operator: PhaseConditionOperator.Active,
          value: null,
          upperValue: null,
          states: null,
        },
        {
          metricDefinitionId: 'm2',
          statusDefinitionId: null,
          operator: PhaseConditionOperator.In,
          value: null,
          upperValue: null,
          states: ['a'],
        },
      ],
      limits: [],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
    };

    const result = toFormValue(phase);

    expect(result.description).toBeNull();
    expect(result.conditions.map((item) => item.sourceId)).toEqual(['s1', 'm2']);
    expect(result.conditions[1].states).toEqual(['a']);
  });
});

describe('hasNoLimitValues', { tags: ['SignalModel'] }, () => {
  it.each([
    ['all four empty', { warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: null }, true],
    ['an alarm high only', { warningLow: null, warningHigh: null, alarmLow: null, alarmHigh: 8.5 }, false],
    ['a zero warning low', { warningLow: 0, warningHigh: null, alarmLow: null, alarmHigh: null }, false],
  ])('hasNoLimitValues_With_%s_ReturnsTheAnswer', (_name, limit, expected) => {
    expect(hasNoLimitValues(limit)).toBe(expected);
  });

  it.each([
    [PhaseConditionOperator.GreaterThan, '>'],
    [PhaseConditionOperator.GreaterOrEqual, '≥'],
    [PhaseConditionOperator.LessThan, '<'],
    [PhaseConditionOperator.LessOrEqual, '≤'],
    [PhaseConditionOperator.Between, '≤ x ≤'],
    [PhaseConditionOperator.In, '∈'],
    [PhaseConditionOperator.IsTrue, '='],
    [PhaseConditionOperator.IsFalse, '='],
  ])('operatorSign_Of%s_Is%s', (operator, sign) => {
    expect(operatorSign(operator)).toBe(sign);
  });

  it.each([PhaseConditionOperator.Active, PhaseConditionOperator.Inactive])('operatorSign_Of%s_IsEmpty', (operator) => {
    expect(operatorSign(operator)).toBe('');
  });

  it('operatorSign_EveryOperator_HasALabelKeyAndASignOrIsAStatusOperator', () => {
    const statusOperators = [PhaseConditionOperator.Active, PhaseConditionOperator.Inactive];

    for (const operator of Object.values(PhaseConditionOperator)) {
      expect(OPERATOR_LABEL_KEYS[operator]).toBeTruthy();
      expect(operatorSign(operator) === '').toBe(statusOperators.includes(operator));
    }
  });
});
