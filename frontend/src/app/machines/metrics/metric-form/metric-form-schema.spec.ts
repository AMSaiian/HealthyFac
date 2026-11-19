import { MetricBehavior, MetricValueKind } from '@machines/metrics/metric-definition';
import {
  metricFormSchema,
  type MetricFormValue,
  WARNING_HIGH_ERROR_KEY,
  WARNING_LOW_ERROR_KEY,
} from '@machines/metrics/metric-form/metric-form-schema';

const VALID: MetricFormValue = {
  key: 'spindle.temperature',
  name: 'Spindle temperature',
  description: null,
  valueKind: MetricValueKind.Numeric,
  unit: 'Temperature|Celsius',
  min: 0,
  max: 100,
  warningLow: 10,
  warningHigh: 70,
  alarmLow: 5,
  alarmHigh: 80,
  behavior: MetricBehavior.Level,
  states: [],
};

function messages(value: MetricFormValue): string[] {
  const result = metricFormSchema.safeParse(value);
  return result.success ? [] : result.error.issues.map((issue) => issue.message);
}

describe('metric form schema limits', () => {
  it('metricFormSchema_LimitsInsideTheirAlarms_HasNoIssue', () => {
    expect(messages(VALID)).toEqual([]);
  });

  it('metricFormSchema_WarningLowEqualToAlarmLow_HasNoIssue', () => {
    expect(messages({ ...VALID, warningLow: 5 })).toEqual([]);
  });

  it('metricFormSchema_WarningLowBelowAlarmLow_ReportsTheWarningLowError', () => {
    expect(messages({ ...VALID, warningLow: 4 })).toEqual([WARNING_LOW_ERROR_KEY]);
  });

  it('metricFormSchema_WarningHighAboveAlarmHigh_ReportsTheWarningHighError', () => {
    expect(messages({ ...VALID, warningHigh: 81 })).toEqual([WARNING_HIGH_ERROR_KEY]);
  });

  it('metricFormSchema_LimitWithoutItsAlarm_HasNoIssue', () => {
    expect(messages({ ...VALID, alarmLow: null, alarmHigh: null, warningLow: 1, warningHigh: 999 })).toEqual([]);
  });

  it('metricFormSchema_BooleanMetricWithStaleLimits_IgnoresTheLimitRules', () => {
    expect(messages({ ...VALID, valueKind: MetricValueKind.Boolean, unit: '', warningLow: 1 })).toEqual([]);
  });
});
