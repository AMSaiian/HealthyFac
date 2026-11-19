import type { PhaseDraftConditionDetail, PhaseDraftDetail, PhaseDraftLimitDetail } from '@machines/drafts/draft';
import {
  metricName,
  type PhaseSourceNames,
  statusCode,
} from '@machines/drafts/phase/phase-draft-review-store/phase-draft-review.store.utils';
import { PhaseConditionOperator } from '@machines/phases/operating-phase';
import { hasNoLimitValues, OPERATOR_LABEL_KEYS, operatorSign } from '@machines/phases/phase-form/phase-form.utils';

const RANGE_SEPARATOR = ' – ';
const STATE_SEPARATOR = ', ';

export interface ConditionRow {
  readonly source: string;
  readonly isStatus: boolean;
  /** The translation key of the operator, or `null` when the draft has none. */
  readonly operatorKey: string | null;
  readonly operatorSign: string;
  readonly valueText: string;
  /** True while the source is a draft that the user has not applied. */
  readonly isWaiting: boolean;
}

export interface LimitRow {
  readonly metric: string;
  readonly warningLow: number | null;
  readonly warningHigh: number | null;
  readonly alarmLow: number | null;
  readonly alarmHigh: number | null;
  readonly hasNoLimits: boolean;
  readonly isWaiting: boolean;
}

function valueOf(condition: PhaseDraftConditionDetail): string {
  switch (condition.operator) {
    case PhaseConditionOperator.Between:
      return `${condition.value ?? ''}${RANGE_SEPARATOR}${condition.upperValue ?? ''}`;
    case PhaseConditionOperator.In:
      return (condition.states ?? []).join(STATE_SEPARATOR);
    case PhaseConditionOperator.GreaterThan:
    case PhaseConditionOperator.GreaterOrEqual:
    case PhaseConditionOperator.LessThan:
    case PhaseConditionOperator.LessOrEqual:
      return condition.value === null ? '' : String(condition.value);
    default:
      return '';
  }
}

/** Builds the rows of the conditions of a phase draft with the names of their sources. */
export function toConditionRows(draft: PhaseDraftDetail, names: PhaseSourceNames): ConditionRow[] {
  return draft.conditions.map((condition) => {
    const isStatus = condition.statusDefinitionId !== null || condition.statusDraftId !== null;
    return {
      source: isStatus ? statusCode(names, condition) : metricName(names, condition),
      isStatus,
      operatorKey:
        condition.operator === null
          ? null
          : (OPERATOR_LABEL_KEYS[condition.operator as PhaseConditionOperator] ?? null),
      operatorSign: condition.operator === null ? '' : operatorSign(condition.operator as PhaseConditionOperator),
      valueText: valueOf(condition),
      isWaiting: condition.metricDraftId !== null || condition.statusDraftId !== null,
    };
  });
}

function toLimitRow(limit: PhaseDraftLimitDetail, names: PhaseSourceNames): LimitRow {
  return {
    metric: metricName(names, limit),
    warningLow: limit.warningLow,
    warningHigh: limit.warningHigh,
    alarmLow: limit.alarmLow,
    alarmHigh: limit.alarmHigh,
    hasNoLimits: hasNoLimitValues(limit),
    isWaiting: limit.metricDraftId !== null,
  };
}

/** Builds the rows of the limit rows of a phase draft with the names of their metrics. */
export function toLimitRows(draft: PhaseDraftDetail, names: PhaseSourceNames): LimitRow[] {
  return draft.limits.map((limit) => toLimitRow(limit, names));
}
