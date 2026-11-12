import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import {
  type AttemptDraftGroup,
  type AttemptDrafts,
  DraftState,
  type MachineDraftDetail,
  type MetricDraftDetail,
  type StatusDraftDetail,
} from '@machines/drafts/draft';
import { type MetricUnitsResponse, MetricValueKind } from '@machines/metrics/metric-definition';
import { unitFromCatalog } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

export enum QuickReason {
  MachineValues = 'DRAFTS.QUICK.NEEDS_MACHINE_VALUES',
  MetricValues = 'DRAFTS.QUICK.NEEDS_METRIC_VALUES',
  UnknownUnit = 'DRAFTS.QUICK.UNKNOWN_UNIT',
  MissingStates = 'DRAFTS.QUICK.NEEDS_STATES',
  StatusValues = 'DRAFTS.QUICK.NEEDS_STATUS_VALUES',
  MachinePending = 'DRAFTS.QUICK.MACHINE_PENDING',
  UnitsLoading = 'DRAFTS.QUICK.UNITS_LOADING',
  NothingToApply = 'DRAFTS.QUICK.ALL_NEED_REVIEW',
}

/** The first wait for a new machine to reach Telemetry. Each retry waits twice as long as the one before. */
const MACHINE_SYNC_FIRST_DELAY_MS = 500;
const MACHINE_SYNC_DELAY_FACTOR = 2;

/** The retries of a call that Telemetry answers with `404` for a new machine: 0.5, 1, 2, 4, and 8 seconds. */
export const MACHINE_SYNC_RETRY_COUNT = 5;

/** The wait of a gateway `429` without a readable `Retry-After` header: the length of the limit window. */
const DEFAULT_RETRY_AFTER_SECONDS = 60;
const RETRY_AFTER_HEADER = 'Retry-After';

/** The most items that one status batch holds (the Telemetry limit). */
export const STATUS_BATCH_MAX_ITEMS = 200;

/**
 * The wait before a retry of a call that got `404` for a machine that Telemetry may not know yet.
 * @param retryCount The number of the retry, from 1.
 */
export function machineSyncDelayMs(retryCount: number): number {
  return MACHINE_SYNC_FIRST_DELAY_MS * MACHINE_SYNC_DELAY_FACTOR ** (retryCount - 1);
}

export function isTooManyRequests(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.TooManyRequests;
}

/** The seconds that the gateway asks to wait, else the length of its limit window. */
export function retryAfterSeconds(error: unknown): number {
  const header = error instanceof HttpErrorResponse ? error.headers.get(RETRY_AFTER_HEADER) : null;
  const seconds = header === null ? Number.NaN : Number(header);
  return Number.isFinite(seconds) && seconds > 0 ? Math.ceil(seconds) : DEFAULT_RETRY_AFTER_SECONDS;
}

/** Splits the items into parts of at most `size` items, in order. */
export function chunk<T>(items: readonly T[], size: number): T[][] {
  const parts: T[][] = [];
  for (let start = 0; start < items.length; start += size) {
    parts.push(items.slice(start, start + size));
  }
  return parts;
}

function isBlank(value: string | null): boolean {
  return value === null || value.trim() === '';
}

/**
 * A create draft needs a name and a serial number: the apply call requires both. An update draft starts from the
 * machine.
 */
export function machineReason(draft: MachineDraftDetail): string | null {
  return draft.isCreate && (isBlank(draft.name) || isBlank(draft.serialNumber)) ? QuickReason.MachineValues : null;
}

/** A metric needs a key and a name, a numeric one a unit of the catalog, and a state set at least one state. */
export function metricReason(draft: MetricDraftDetail, catalog: MetricUnitsResponse | undefined): string | null {
  if (isBlank(draft.key) || isBlank(draft.name)) {
    return QuickReason.MetricValues;
  }
  if (draft.valueKind === MetricValueKind.StateSet && (draft.states === null || draft.states.length === 0)) {
    return QuickReason.MissingStates;
  }
  const isNumeric = draft.valueKind === null || draft.valueKind === MetricValueKind.Numeric;
  if (isNumeric) {
    if (catalog === undefined) {
      return QuickReason.UnitsLoading;
    }
    return unitFromCatalog(draft.quantityKindName, draft.unitName, catalog) === '' ? QuickReason.UnknownUnit : null;
  }
  return null;
}

export function statusReason(draft: StatusDraftDetail): string | null {
  return isBlank(draft.code) ? QuickReason.StatusValues : null;
}

function isMachineDraftPendingCreate(group: AttemptDraftGroup): boolean {
  return group.machineDraft?.isCreate === true && group.machineDraft.state === DraftState.Pending;
}

/** Whether a child draft has to wait for the machine: the create draft is pending, or the group has no machine yet. */
export function isMachineMissing(group: AttemptDraftGroup): boolean {
  return isMachineDraftPendingCreate(group) || group.machineId === null;
}

/**
 * The reason that **Apply** is off for each pending draft of the attempt (ID to translation key), or `null` for a
 * draft that applies as it is. A child draft of a pending create draft waits for its machine.
 */
export function needsReviewReasons(
  drafts: AttemptDrafts,
  catalog: MetricUnitsResponse | undefined,
): Readonly<Record<string, string | null>> {
  const reasons: Record<string, string | null> = {};
  for (const group of drafts.groups) {
    if (group.machineDraft?.state === DraftState.Pending) {
      reasons[group.machineDraft.id] = machineReason(group.machineDraft);
    }
    const childWait = isMachineMissing(group) ? QuickReason.MachinePending : null;
    for (const metric of group.metrics) {
      if (metric.state === DraftState.Pending) {
        reasons[metric.id] = childWait ?? metricReason(metric, catalog);
      }
    }
    for (const status of group.statuses) {
      if (status.state === DraftState.Pending) {
        reasons[status.id] = childWait ?? statusReason(status);
      }
    }
  }
  return reasons;
}

/** The pending metric drafts of the group that apply on their own (the machine exists or is applied first). */
export function applicableMetrics(
  group: AttemptDraftGroup,
  catalog: MetricUnitsResponse | undefined,
): MetricDraftDetail[] {
  return group.metrics.filter((draft) => draft.state === DraftState.Pending && metricReason(draft, catalog) === null);
}

export function applicableStatuses(group: AttemptDraftGroup): StatusDraftDetail[] {
  return group.statuses.filter((draft) => draft.state === DraftState.Pending && statusReason(draft) === null);
}

/**
 * The reason that **Apply all** is off for a group, or `null`. A pending machine draft that needs review blocks the
 * group, because its children depend on it. Else the group needs at least one draft that applies.
 */
export function groupReason(group: AttemptDraftGroup, catalog: MetricUnitsResponse | undefined): string | null {
  const machine = group.machineDraft;
  const isMachinePending = machine !== null && machine.state === DraftState.Pending;
  if (isMachinePending) {
    return machineReason(machine);
  }
  if (group.machineId === null) {
    return QuickReason.MachinePending;
  }
  const hasApplicable = applicableMetrics(group, catalog).length + applicableStatuses(group).length > 0;
  return hasApplicable ? null : QuickReason.NothingToApply;
}
