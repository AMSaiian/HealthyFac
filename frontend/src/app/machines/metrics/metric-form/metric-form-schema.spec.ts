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

describe('metric form schema limits', { tags: ['SignalModel'] }, () => {
  it.each<[string, Partial<MetricFormValue>, string[]]>([
    ['LimitsInsideTheirAlarms_HasNoIssue', {}, []],
    ['WarningLowEqualToAlarmLow_HasNoIssue', { warningLow: 5 }, []],
    ['WarningLowBelowAlarmLow_ReportsTheWarningLowError', { warningLow: 4 }, [WARNING_LOW_ERROR_KEY]],
    ['WarningHighAboveAlarmHigh_ReportsTheWarningHighError', { warningHigh: 81 }, [WARNING_HIGH_ERROR_KEY]],
    ['LimitWithoutItsAlarm_HasNoIssue', { alarmLow: null, alarmHigh: null, warningLow: 1, warningHigh: 999 }, []],
    [
      'BooleanMetricWithStaleLimits_IgnoresTheLimitRules',
      { valueKind: MetricValueKind.Boolean, unit: '', warningLow: 1 },
      [],
    ],
  ])('metricFormSchema_%s', (_name, overrides, expected) => {
    expect(messages({ ...VALID, ...overrides })).toEqual(expected);
  });
});
