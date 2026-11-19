import type {
  AttemptDrafts,
  PhaseDraftConditionDetail,
  PhaseDraftDetail,
  PhaseDraftLimitDetail,
} from '@machines/drafts/draft';
import type { MetricDefinition } from '@machines/metrics/metric-definition';
import {
  type OperatingPhaseBody,
  type PhaseCondition,
  PhaseConditionOperator,
  type PhaseLimit,
} from '@machines/phases/operating-phase';
import type { PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import type { StatusDefinition } from '@machines/statuses/status-definition';

/** Why a phase draft cannot apply as it is (a translation key). */
export enum PhaseApplyBlock {
  /** A condition or a limit row still names a metric or status draft that the user has not applied. */
  WaitsForDrafts = 'DRAFTS.PHASE.WAITS_FOR_DRAFTS',
  /** The draft lacks a value that the phase needs: a code, a name, a priority, a tracking flag, a condition. */
  NeedsValues = 'DRAFTS.PHASE.NEEDS_VALUES',
}

/** The names that the review shows for the sources of a phase draft, by definition ID and by draft ID. */
export interface PhaseSourceNames {
  readonly metrics: ReadonlyMap<string, string>;
  readonly statuses: ReadonlyMap<string, string>;
}

const OPERATORS = new Set<string>(Object.values(PhaseConditionOperator));

/** Finds the phase draft with the given ID in the drafts of an attempt. */
export function findPhaseDraft(drafts: AttemptDrafts, draftId: string): PhaseDraftDetail | null {
  return drafts.groups.flatMap((group) => group.phases).find((phase) => phase.id === draftId) ?? null;
}

/** Tells whether a condition or a limit row of the draft still names a draft of a metric or a status. */
export function waitsForDrafts(draft: PhaseDraftDetail): boolean {
  return (
    draft.conditions.some((condition) => condition.metricDraftId !== null || condition.statusDraftId !== null) ||
    draft.limits.some((limit) => limit.metricDraftId !== null)
  );
}

function isBlank(value: string | null): boolean {
  return value === null || value.trim() === '';
}

function isConditionComplete(condition: PhaseDraftConditionDetail): boolean {
  const hasSource = condition.metricDefinitionId !== null || condition.statusDefinitionId !== null;
  return hasSource && condition.operator !== null && OPERATORS.has(condition.operator);
}

/**
 * The reason that **Apply** is off for a phase draft, or `null` when its values apply as they are. A draft that waits
 * for a metric or status draft comes first, because the backend answers `422` for it.
 */
export function phaseApplyBlock(draft: PhaseDraftDetail): PhaseApplyBlock | null {
  if (waitsForDrafts(draft)) {
    return PhaseApplyBlock.WaitsForDrafts;
  }
  const isComplete =
    !isBlank(draft.code) &&
    !isBlank(draft.name) &&
    draft.priority !== null &&
    draft.isTracked !== null &&
    draft.conditions.length > 0 &&
    draft.conditions.every(isConditionComplete) &&
    draft.limits.every((limit) => limit.metricDefinitionId !== null);
  return isComplete ? null : PhaseApplyBlock.NeedsValues;
}

/**
 * The reason that **Apply** is off in the editable review, or `null`. A draft that waits for a draft stays read-only.
 * Else the priority and the valid form are needed: the form checks the other values.
 */
export function phaseReviewBlock(
  draft: PhaseDraftDetail,
  priority: number | null,
  isFormValid: boolean,
): PhaseApplyBlock | null {
  if (waitsForDrafts(draft)) {
    return PhaseApplyBlock.WaitsForDrafts;
  }
  return priority === null || !isFormValid ? PhaseApplyBlock.NeedsValues : null;
}

/** The start value of the phase form for a draft: a missing value becomes the empty value of the form. */
export function toDraftFormValue(draft: PhaseDraftDetail): PhaseFormValue {
  return {
    code: draft.code ?? '',
    name: draft.name ?? '',
    description: draft.description,
    isTracked: draft.isTracked ?? true,
    conditions: draft.conditions.map((condition) => ({
      sourceId: condition.statusDefinitionId ?? condition.metricDefinitionId ?? '',
      operator: (condition.operator ?? '') as PhaseConditionOperator,
      value: condition.value,
      upperValue: condition.upperValue,
      states: [...(condition.states ?? [])],
    })),
    limits: draft.limits.map((limit) => ({
      metricDefinitionId: limit.metricDefinitionId ?? '',
      warningLow: limit.warningLow,
      warningHigh: limit.warningHigh,
      alarmLow: limit.alarmLow,
      alarmHigh: limit.alarmHigh,
    })),
  };
}

function toCondition(condition: PhaseDraftConditionDetail): PhaseCondition {
  return {
    metricDefinitionId: condition.metricDefinitionId,
    statusDefinitionId: condition.statusDefinitionId,
    operator: condition.operator as PhaseConditionOperator,
    value: condition.value,
    upperValue: condition.upperValue,
    states: condition.states,
  };
}

function toLimit(limit: PhaseDraftLimitDetail): PhaseLimit {
  return {
    metricDefinitionId: limit.metricDefinitionId ?? '',
    warningLow: limit.warningLow,
    warningHigh: limit.warningHigh,
    alarmLow: limit.alarmLow,
    alarmHigh: limit.alarmHigh,
  };
}

/** Builds the apply body from the draft, or `null` when the draft cannot apply as it is (see `phaseApplyBlock`). */
export function toPhaseBody(draft: PhaseDraftDetail): OperatingPhaseBody | null {
  if (phaseApplyBlock(draft) !== null) {
    return null;
  }
  const description = draft.description;
  return {
    code: (draft.code ?? '').trim(),
    name: (draft.name ?? '').trim(),
    description: description === null || description.trim() === '' ? null : description,
    priority: draft.priority ?? 0,
    isTracked: draft.isTracked ?? false,
    conditions: draft.conditions.map(toCondition),
    limits: draft.limits.map(toLimit),
  };
}

/**
 * Collects the names of the sources of the draft: the metric and status definitions of the machine by ID, and the
 * metric and status drafts of the attempt by draft ID (their key or code).
 */
export function toSourceNames(
  drafts: AttemptDrafts,
  metrics: readonly MetricDefinition[],
  statuses: readonly StatusDefinition[],
): PhaseSourceNames {
  const metricNames = new Map<string, string>();
  const statusNames = new Map<string, string>();
  for (const metric of metrics) {
    metricNames.set(metric.id, metric.name);
  }
  for (const status of statuses) {
    statusNames.set(status.id, status.code);
  }
  for (const group of drafts.groups) {
    for (const metric of group.metrics) {
      metricNames.set(metric.id, metric.name ?? metric.key ?? '');
    }
    for (const status of group.statuses) {
      statusNames.set(status.id, status.code ?? '');
    }
  }
  return { metrics: metricNames, statuses: statusNames };
}

/** The name of the metric of a condition or a limit row: a definition or a draft. Unknown sources give an empty text. */
export function metricName(
  names: PhaseSourceNames,
  source: { readonly metricDefinitionId: string | null; readonly metricDraftId: string | null },
): string {
  const id = source.metricDefinitionId ?? source.metricDraftId;
  return id === null ? '' : (names.metrics.get(id) ?? '');
}

/** The code of the status of a condition: a definition or a draft. */
export function statusCode(names: PhaseSourceNames, condition: PhaseDraftConditionDetail): string {
  const id = condition.statusDefinitionId ?? condition.statusDraftId;
  return id === null ? '' : (names.statuses.get(id) ?? '');
}
