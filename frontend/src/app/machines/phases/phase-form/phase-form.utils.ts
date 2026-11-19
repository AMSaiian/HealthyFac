import { type MetricDefinition, MetricValueKind } from '@machines/metrics/metric-definition';
import {
  ConditionSourceKind,
  type OperatingPhase,
  type OperatingPhaseBody,
  type PhaseCondition,
  PhaseConditionOperator,
  type PhaseLimit,
} from '@machines/phases/operating-phase';
import type {
  PhaseConditionFormValue,
  PhaseFormValue,
  PhaseLimitFormValue,
} from '@machines/phases/phase-form/phase-form-schema';
import type { StatusDefinition } from '@machines/statuses/status-definition';

export const EMPTY_FORM_VALUE: PhaseFormValue = {
  code: '',
  name: '',
  description: null,
  isTracked: true,
  conditions: [],
  limits: [],
};

const OPERATORS_BY_KIND: Record<ConditionSourceKind, readonly PhaseConditionOperator[]> = {
  [ConditionSourceKind.Status]: [PhaseConditionOperator.Active, PhaseConditionOperator.Inactive],
  [ConditionSourceKind.Numeric]: [
    PhaseConditionOperator.GreaterThan,
    PhaseConditionOperator.GreaterOrEqual,
    PhaseConditionOperator.LessThan,
    PhaseConditionOperator.LessOrEqual,
    PhaseConditionOperator.Between,
  ],
  [ConditionSourceKind.Boolean]: [PhaseConditionOperator.IsTrue, PhaseConditionOperator.IsFalse],
  [ConditionSourceKind.StateSet]: [PhaseConditionOperator.In],
};

export const OPERATOR_LABEL_KEYS: Readonly<Record<PhaseConditionOperator, string>> = {
  [PhaseConditionOperator.Active]: 'PHASES.OPERATORS.ACTIVE',
  [PhaseConditionOperator.Inactive]: 'PHASES.OPERATORS.INACTIVE',
  [PhaseConditionOperator.GreaterThan]: 'PHASES.OPERATORS.GREATER_THAN',
  [PhaseConditionOperator.GreaterOrEqual]: 'PHASES.OPERATORS.GREATER_OR_EQUAL',
  [PhaseConditionOperator.LessThan]: 'PHASES.OPERATORS.LESS_THAN',
  [PhaseConditionOperator.LessOrEqual]: 'PHASES.OPERATORS.LESS_OR_EQUAL',
  [PhaseConditionOperator.Between]: 'PHASES.OPERATORS.BETWEEN',
  [PhaseConditionOperator.IsTrue]: 'PHASES.OPERATORS.IS_TRUE',
  [PhaseConditionOperator.IsFalse]: 'PHASES.OPERATORS.IS_FALSE',
  [PhaseConditionOperator.In]: 'PHASES.OPERATORS.IN',
};

/**
 * The sign before the text of an operator. The signs are the same in every language, so they live here and not in the
 * translation files. `active` and `inactive` have no sign.
 */
export const OPERATOR_SIGNS: Readonly<Partial<Record<PhaseConditionOperator, string>>> = {
  [PhaseConditionOperator.GreaterThan]: '>',
  [PhaseConditionOperator.GreaterOrEqual]: '≥',
  [PhaseConditionOperator.LessThan]: '<',
  [PhaseConditionOperator.LessOrEqual]: '≤',
  [PhaseConditionOperator.Between]: '≤ x ≤',
  [PhaseConditionOperator.In]: '∈',
  [PhaseConditionOperator.IsTrue]: '=',
  [PhaseConditionOperator.IsFalse]: '=',
};

/** The sign of an operator, or an empty text for an operator without a sign. */
export function operatorSign(operator: PhaseConditionOperator): string {
  return OPERATOR_SIGNS[operator] ?? '';
}

/** The operators that a condition offers for the kind of its source. */
export function operatorsFor(kind: ConditionSourceKind): readonly PhaseConditionOperator[] {
  return OPERATORS_BY_KIND[kind];
}

/** The kind of a condition source for a metric of the value kind. */
export function sourceKindOfMetric(valueKind: MetricValueKind): ConditionSourceKind {
  switch (valueKind) {
    case MetricValueKind.Numeric:
      return ConditionSourceKind.Numeric;
    case MetricValueKind.Boolean:
      return ConditionSourceKind.Boolean;
    case MetricValueKind.StateSet:
      return ConditionSourceKind.StateSet;
  }
}

/** One entry of the source select of a condition: a status or a metric. */
export interface ConditionSourceOption {
  readonly id: string;
  readonly kind: ConditionSourceKind;
  readonly label: string;
}

export function toSourceOptions(
  statuses: readonly StatusDefinition[],
  metrics: readonly MetricDefinition[],
): readonly ConditionSourceOption[] {
  return [
    ...statuses.map((status) => ({ id: status.id, kind: ConditionSourceKind.Status, label: status.code })),
    ...metrics.map((metric) => ({ id: metric.id, kind: sourceKindOfMetric(metric.valueKind), label: metric.name })),
  ];
}

/** The kind of the source with the ID, or `null` when no option has it. */
export function kindOfSource(sourceId: string, options: readonly ConditionSourceOption[]): ConditionSourceKind | null {
  return options.find((option) => option.id === sourceId)?.kind ?? null;
}

/** A new condition for the source: the first operator of its kind, with empty values. */
export function newCondition(source: ConditionSourceOption | null): PhaseConditionFormValue {
  return {
    sourceId: source?.id ?? '',
    operator: operatorsFor(source?.kind ?? ConditionSourceKind.Status)[0],
    value: null,
    upperValue: null,
    states: [],
  };
}

/**
 * The condition with an operator that its source offers. A source change resets the values, and the operator stays
 * when the new kind offers it. A condition that is already valid stays the same object.
 */
export function normalizedCondition(
  condition: PhaseConditionFormValue,
  options: readonly ConditionSourceOption[],
): PhaseConditionFormValue {
  const kind = kindOfSource(condition.sourceId, options);
  if (kind === null || operatorsFor(kind).includes(condition.operator)) {
    return condition;
  }
  return { ...condition, operator: operatorsFor(kind)[0], value: null, upperValue: null, states: [] };
}

/** The metrics that a limit row may pick: the numeric ones that no condition reads. */
export function limitMetricOptions(
  metrics: readonly MetricDefinition[],
  conditions: readonly PhaseConditionFormValue[],
): readonly MetricDefinition[] {
  const used = new Set(conditions.map((condition) => condition.sourceId));
  return metrics.filter((metric) => metric.valueKind === MetricValueKind.Numeric && !used.has(metric.id));
}

/** A new limit row for the metric. It copies the limits of the metric definition, and sets nothing when it has none. */
export function newLimit(metric: MetricDefinition): PhaseLimitFormValue {
  return {
    metricDefinitionId: metric.id,
    warningLow: metric.warningLow ?? null,
    warningHigh: metric.warningHigh ?? null,
    alarmLow: metric.alarmLow ?? null,
    alarmHigh: metric.alarmHigh ?? null,
  };
}

/** The names of the metrics that a condition reads. Their drift cannot show inside the phase. */
export function maskedMetricNames(
  conditions: readonly PhaseConditionFormValue[],
  metrics: readonly MetricDefinition[],
): readonly string[] {
  const ids = new Set(conditions.map((condition) => condition.sourceId));
  return metrics.filter((metric) => ids.has(metric.id)).map((metric) => metric.name);
}

export function toFormValue(phase: OperatingPhase): PhaseFormValue {
  return {
    code: phase.code,
    name: phase.name,
    description: phase.description,
    isTracked: phase.isTracked,
    conditions: phase.conditions.map((condition) => ({
      sourceId: condition.statusDefinitionId ?? condition.metricDefinitionId ?? '',
      operator: condition.operator,
      value: condition.value,
      upperValue: condition.upperValue,
      states: [...(condition.states ?? [])],
    })),
    limits: phase.limits.map((limit) => ({ ...limit })),
  };
}

function toCondition(condition: PhaseConditionFormValue, statusIds: ReadonlySet<string>): PhaseCondition {
  const isStatus = statusIds.has(condition.sourceId);
  return {
    metricDefinitionId: isStatus ? null : condition.sourceId,
    statusDefinitionId: isStatus ? condition.sourceId : null,
    operator: condition.operator,
    value: condition.value,
    upperValue: condition.operator === PhaseConditionOperator.Between ? condition.upperValue : null,
    states: condition.operator === PhaseConditionOperator.In ? condition.states : null,
  };
}

/** Tells whether a limit row sets no value: the phase then has no limits for the metric. */
export function hasNoLimitValues(limit: {
  readonly warningLow: number | null;
  readonly warningHigh: number | null;
  readonly alarmLow: number | null;
  readonly alarmHigh: number | null;
}): boolean {
  return limit.warningLow === null && limit.warningHigh === null && limit.alarmLow === null && limit.alarmHigh === null;
}

/** Builds the request body. The name and code are trimmed, and an empty description becomes `null`. */
export function toBody(value: PhaseFormValue, priority: number, statusIds: ReadonlySet<string>): OperatingPhaseBody {
  const description = value.description;
  return {
    code: value.code.trim(),
    name: value.name.trim(),
    description: description === null || description.trim() === '' ? null : description,
    priority,
    isTracked: value.isTracked,
    conditions: value.conditions.map((condition) => toCondition(condition, statusIds)),
    limits: value.limits.map((limit): PhaseLimit => ({ ...limit })),
  };
}

/** The priority of a new phase: one above the highest stored one. */
export function nextPriority(phases: readonly OperatingPhase[]): number {
  return phases.reduce((highest, phase) => Math.max(highest, phase.priority), -1) + 1;
}
