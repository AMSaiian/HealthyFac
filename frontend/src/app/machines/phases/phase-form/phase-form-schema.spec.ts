import { PHASE_DESCRIPTION_MAX_LENGTH } from '@machines/phases/operating-phase-limits';
import { PhaseConditionOperator } from '@machines/phases/operating-phase';
import {
  CONDITIONS_REQUIRED_ERROR_KEY,
  hasComparisonValue,
  LIMIT_METRIC_DUPLICATED_ERROR_KEY,
  LIMIT_METRIC_IN_CONDITION_ERROR_KEY,
  phaseFormSchema,
  STATES_REQUIRED_ERROR_KEY,
  TOO_LONG_ERROR_KEY,
  UPPER_VALUE_ERROR_KEY,
  VALUE_REQUIRED_ERROR_KEY,
  WARNING_HIGH_ERROR_KEY,
  WARNING_LOW_ERROR_KEY,
} from '@machines/phases/phase-form/phase-form-schema';
import { EMPTY_FORM_VALUE } from '@machines/phases/phase-form/phase-form.utils';

function messages(value: unknown): string[] {
  const result = phaseFormSchema.safeParse(value);
  return result.success ? [] : result.error.issues.map((issue) => issue.message);
}

const limit = (extra: object): object => ({
  metricDefinitionId: 'm1',
  warningLow: null,
  warningHigh: null,
  alarmLow: null,
  alarmHigh: null,
  ...extra,
});

const condition = (extra: object): object => ({
  sourceId: 'm1',
  operator: PhaseConditionOperator.GreaterThan,
  value: 1,
  upperValue: null,
  states: [],
  ...extra,
});

const VALID = { ...EMPTY_FORM_VALUE, code: 'idle', name: 'Idle', conditions: [condition({ sourceId: 'c1' })] };

describe('phase form schema', () => {
  it('phaseFormSchema_WithADescriptionOverTheRichTextLimit_IsTooLong', () => {
    const description = `<p>${'a'.repeat(PHASE_DESCRIPTION_MAX_LENGTH + 1)}</p>`;

    expect(messages({ ...VALID, description })).toContain(TOO_LONG_ERROR_KEY);
  });

  it('phaseFormSchema_WithAnEmptyRichTextDescription_IsValid', () => {
    expect(messages({ ...VALID, description: null })).toEqual([]);
  });

  it('hasComparisonValue_EachOperator_IsTrueOnlyForNumericComparisons', () => {
    const withValue = Object.values(PhaseConditionOperator).filter(hasComparisonValue);

    expect(withValue).toEqual([
      PhaseConditionOperator.GreaterThan,
      PhaseConditionOperator.GreaterOrEqual,
      PhaseConditionOperator.LessThan,
      PhaseConditionOperator.LessOrEqual,
      PhaseConditionOperator.Between,
    ]);
  });

  it('phaseFormSchema_ValidPhase_HasNoIssue', () => {
    expect(messages(VALID)).toEqual([]);
  });

  it('phaseFormSchema_PhaseWithoutConditions_ReportsThatAConditionIsRequired', () => {
    expect(messages({ ...VALID, conditions: [] })).toContain(CONDITIONS_REQUIRED_ERROR_KEY);
  });

  it('phaseFormSchema_ComparisonWithoutValue_ReportsTheValueIsRequired', () => {
    expect(messages({ ...VALID, conditions: [condition({ value: null })] })).toContain(VALUE_REQUIRED_ERROR_KEY);
  });

  it('phaseFormSchema_BetweenWithUpperValueNotAboveTheValue_ReportsTheUpperValueError', () => {
    const between = condition({ operator: PhaseConditionOperator.Between, value: 5, upperValue: 5 });

    expect(messages({ ...VALID, conditions: [between] })).toContain(UPPER_VALUE_ERROR_KEY);
  });

  it('phaseFormSchema_InWithoutStates_ReportsThatStatesAreRequired', () => {
    const inCondition = condition({ operator: PhaseConditionOperator.In, value: null });

    expect(messages({ ...VALID, conditions: [inCondition] })).toContain(STATES_REQUIRED_ERROR_KEY);
  });

  it('phaseFormSchema_WarningBelowAlarmLow_ReportsTheWarningLowError', () => {
    expect(messages({ ...VALID, limits: [limit({ warningLow: 1, alarmLow: 2 })] })).toContain(WARNING_LOW_ERROR_KEY);
  });

  it('phaseFormSchema_WarningAboveAlarmHigh_ReportsTheWarningHighError', () => {
    expect(messages({ ...VALID, limits: [limit({ warningHigh: 9, alarmHigh: 8 })] })).toContain(WARNING_HIGH_ERROR_KEY);
  });

  it('phaseFormSchema_TwoLimitsOfOneMetric_ReportsTheDuplicate', () => {
    expect(messages({ ...VALID, limits: [limit({}), limit({})] })).toContain(LIMIT_METRIC_DUPLICATED_ERROR_KEY);
  });

  it('phaseFormSchema_LimitOfAMetricThatAConditionReads_ReportsTheMasking', () => {
    expect(messages({ ...VALID, conditions: [condition({})], limits: [limit({})] })).toContain(
      LIMIT_METRIC_IN_CONDITION_ERROR_KEY,
    );
  });
});
