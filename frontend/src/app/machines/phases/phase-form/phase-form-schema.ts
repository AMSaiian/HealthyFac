import {
  array,
  boolean,
  type infer as Infer,
  maxLength,
  minLength,
  nullable,
  number,
  object,
  refine,
  regex,
  string,
  trim,
  enum as zodEnum,
} from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { PhaseConditionOperator } from '@machines/phases/operating-phase';
import {
  PHASE_CODE_MAX_LENGTH,
  PHASE_CODE_PATTERN,
  PHASE_DESCRIPTION_MAX_LENGTH,
  PHASE_NAME_MAX_LENGTH,
} from '@machines/phases/operating-phase-limits';

export const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';
export const CODE_INVALID_ERROR_KEY = 'PHASES.FORM.ERRORS.CODE_INVALID';
export const VALUE_REQUIRED_ERROR_KEY = 'PHASES.FORM.ERRORS.VALUE_REQUIRED';
export const UPPER_VALUE_ERROR_KEY = 'PHASES.FORM.ERRORS.UPPER_VALUE';
export const CONDITIONS_REQUIRED_ERROR_KEY = 'PHASES.FORM.ERRORS.CONDITIONS_REQUIRED';
export const STATES_REQUIRED_ERROR_KEY = 'PHASES.FORM.ERRORS.STATES_REQUIRED';
export const WARNING_LOW_ERROR_KEY = 'PHASES.FORM.ERRORS.WARNING_LOW';
export const WARNING_HIGH_ERROR_KEY = 'PHASES.FORM.ERRORS.WARNING_HIGH';
export const LIMIT_METRIC_DUPLICATED_ERROR_KEY = 'PHASES.FORM.ERRORS.LIMIT_METRIC_DUPLICATED';
export const LIMIT_METRIC_IN_CONDITION_ERROR_KEY = 'PHASES.FORM.ERRORS.LIMIT_METRIC_IN_CONDITION';

const COMPARISON_OPERATORS: readonly PhaseConditionOperator[] = [
  PhaseConditionOperator.GreaterThan,
  PhaseConditionOperator.GreaterOrEqual,
  PhaseConditionOperator.LessThan,
  PhaseConditionOperator.LessOrEqual,
  PhaseConditionOperator.Between,
];

/** Tells whether the operator compares a metric with a number. */
export function hasComparisonValue(operator: PhaseConditionOperator): boolean {
  return COMPARISON_OPERATORS.includes(operator);
}

const conditionSchema = object({
  sourceId: string().check(minLength(1, REQUIRED_ERROR_KEY)),
  operator: zodEnum(PhaseConditionOperator),
  value: nullable(number()),
  upperValue: nullable(number()),
  states: array(string()),
}).check(
  refine((condition) => !hasComparisonValue(condition.operator) || condition.value !== null, {
    path: ['value'],
    error: VALUE_REQUIRED_ERROR_KEY,
  }),
  refine(
    (condition) =>
      condition.operator !== PhaseConditionOperator.Between ||
      (condition.upperValue !== null && condition.value !== null && condition.upperValue > condition.value),
    { path: ['upperValue'], error: UPPER_VALUE_ERROR_KEY },
  ),
  refine((condition) => condition.operator !== PhaseConditionOperator.In || condition.states.length > 0, {
    path: ['states'],
    error: STATES_REQUIRED_ERROR_KEY,
  }),
);

const limitSchema = object({
  metricDefinitionId: string().check(minLength(1, REQUIRED_ERROR_KEY)),
  warningLow: nullable(number()),
  warningHigh: nullable(number()),
  alarmLow: nullable(number()),
  alarmHigh: nullable(number()),
}).check(
  refine((limit) => limit.warningLow === null || limit.alarmLow === null || limit.warningLow >= limit.alarmLow, {
    path: ['warningLow'],
    error: WARNING_LOW_ERROR_KEY,
  }),
  refine((limit) => limit.warningHigh === null || limit.alarmHigh === null || limit.warningHigh <= limit.alarmHigh, {
    path: ['warningHigh'],
    error: WARNING_HIGH_ERROR_KEY,
  }),
);

/** The one schema of an operating phase. The dialog form validates with it. The messages are translation keys. */
export const phaseFormSchema = object({
  code: string().check(
    trim(),
    minLength(1, REQUIRED_ERROR_KEY),
    maxLength(PHASE_CODE_MAX_LENGTH, TOO_LONG_ERROR_KEY),
    regex(PHASE_CODE_PATTERN, CODE_INVALID_ERROR_KEY),
  ),
  name: string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(PHASE_NAME_MAX_LENGTH, TOO_LONG_ERROR_KEY)),
  description: richTextSchema(PHASE_DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  isTracked: boolean(),
  conditions: array(conditionSchema).check(minLength(1, CONDITIONS_REQUIRED_ERROR_KEY)),
  limits: array(limitSchema),
}).check(
  refine((phase) => new Set(phase.limits.map((limit) => limit.metricDefinitionId)).size === phase.limits.length, {
    path: ['limits'],
    error: LIMIT_METRIC_DUPLICATED_ERROR_KEY,
  }),
  refine(
    (phase) => {
      const conditionIds = new Set(phase.conditions.map((condition) => condition.sourceId));
      return phase.limits.every((limit) => !conditionIds.has(limit.metricDefinitionId));
    },
    { path: ['limits'], error: LIMIT_METRIC_IN_CONDITION_ERROR_KEY },
  ),
);

export type PhaseFormValue = Infer<typeof phaseFormSchema>;
export type PhaseConditionFormValue = PhaseFormValue['conditions'][number];
export type PhaseLimitFormValue = PhaseFormValue['limits'][number];
