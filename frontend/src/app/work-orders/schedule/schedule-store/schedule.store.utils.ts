import { type Axis, segmentOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import type { ScheduleBar, ScheduleBlock, ScheduleRow } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import { STATUS_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { type ScheduleChangeBody, type WorkOrderPriority, WorkOrderStatus } from '@work-orders/work-order';
import { SCHEDULE_APPLY_MAX_CHANGES } from '@work-orders/work-order-limits';

/** A work order as the server last showed it: the base of every pending change. */
export interface KnownWorkOrder {
  readonly id: string;
  readonly title: string;
  readonly status: WorkOrderStatus;
  readonly priority: WorkOrderPriority;
  readonly rowVersion: number;
  readonly estimateMinutes: number;
  readonly assigneeId: string | null;
  readonly plannedStartLocal: string | null;
}

/** The target state of one work order. One pending entry exists for each work order that differs from the server. */
export type PendingChange = ScheduleChangeBody;

export type ChangeTarget = Pick<PendingChange, 'assigneeId' | 'plannedStartLocal' | 'estimateMinutes'>;

export interface StagingWorkOrder {
  readonly id: string;
  readonly title: string;
  readonly priority: WorkOrderPriority;
  readonly estimateMinutes: number;
}

function isSameStart(first: string | null, second: string | null): boolean {
  if (first === null || second === null) {
    return first === second;
  }
  return Temporal.PlainDateTime.compare(Temporal.PlainDateTime.from(first), Temporal.PlainDateTime.from(second)) === 0;
}

/** The local start as the apply body wants it: `yyyy-MM-ddTHH:mm:ss` in the company zone. */
export function localStartOf(instant: Temporal.Instant, timeZoneId: string): string {
  return instant.toZonedDateTimeISO(timeZoneId).toPlainDateTime().toString({ smallestUnit: 'second' });
}

/** The target that the server state already holds. */
export function targetOf(known: KnownWorkOrder): ChangeTarget {
  return {
    assigneeId: known.assigneeId,
    plannedStartLocal: known.plannedStartLocal,
    estimateMinutes: known.estimateMinutes,
  };
}

/** Whether a target differs from the server state of the work order. */
export function isChanged(known: KnownWorkOrder, target: ChangeTarget): boolean {
  return (
    known.assigneeId !== target.assigneeId ||
    known.estimateMinutes !== target.estimateMinutes ||
    !isSameStart(known.plannedStartLocal, target.plannedStartLocal)
  );
}

/**
 * Puts a new target of a work order into the pending list. A target equal to the server state removes the entry.
 * The list keeps one entry for each work order, in the order of the first change.
 */
export function withTarget(
  pending: readonly PendingChange[],
  known: KnownWorkOrder,
  target: ChangeTarget,
): readonly PendingChange[] {
  const others = pending.filter((entry) => entry.workOrderId !== known.id);
  if (!isChanged(known, target)) {
    return others;
  }
  const entry: PendingChange = { workOrderId: known.id, rowVersion: known.rowVersion, ...target };
  const index = pending.findIndex((candidate) => candidate.workOrderId === known.id);
  return index === -1
    ? [...others, entry]
    : pending.map((candidate, position) => (position === index ? entry : candidate));
}

function barOf(known: KnownWorkOrder, change: PendingChange, timeZoneId: string): ScheduleBar | null {
  if (change.plannedStartLocal === null) {
    return null;
  }
  const start = Temporal.PlainDateTime.from(change.plannedStartLocal).toZonedDateTime(timeZoneId).toInstant();
  const status = known.status === WorkOrderStatus.Backlog ? WorkOrderStatus.Staged : known.status;
  return {
    id: known.id,
    title: known.title,
    start,
    end: start.add({ minutes: change.estimateMinutes }),
    statusKey: STATUS_LABEL_KEYS[status],
    colorClass: `hf-status-${status}`,
    isLocked: status === WorkOrderStatus.Completed,
    isRunning: status === WorkOrderStatus.InProgress,
    plannedEnd: start.add({ minutes: change.estimateMinutes }),
  };
}

/**
 * Draws the pending changes into the rows of one day: a changed work order leaves every row, then it is drawn again in
 * the row of its new assignee when its new slot touches the axis. A bar that arrives from another day or from the
 * staging area is drawn from the known data.
 */
export function applyPending(
  rows: readonly ScheduleRow[],
  pending: readonly PendingChange[],
  known: ReadonlyMap<string, KnownWorkOrder>,
  axis: Axis,
  timeZoneId: string,
): ScheduleRow[] {
  if (pending.length === 0) {
    return [...rows];
  }
  const changedIds = new Set(pending.map((entry) => entry.workOrderId));
  const stripped = rows.map((row) => ({ ...row, bars: row.bars.filter((bar) => !changedIds.has(bar.id)) }));
  for (const change of pending) {
    const base = known.get(change.workOrderId);
    const bar = base === undefined ? null : barOf(base, change, timeZoneId);
    const index = stripped.findIndex((row) => row.id === change.assigneeId);
    if (bar !== null && index !== -1 && segmentOf(bar.start, bar.end, axis) !== null) {
      stripped[index] = { ...stripped[index], bars: [...stripped[index].bars, bar] };
    }
  }
  return stripped;
}

function isInsideOneBlock(bar: ScheduleBar, blocks: readonly ScheduleBlock[]): boolean {
  return blocks.some(
    (block) =>
      Temporal.Instant.compare(block.start, bar.start) <= 0 && Temporal.Instant.compare(bar.end, block.end) <= 0,
  );
}

function isOverlapping(first: ScheduleBar, second: ScheduleBar): boolean {
  return Temporal.Instant.compare(first.start, second.end) < 0 && Temporal.Instant.compare(second.start, first.end) < 0;
}

/**
 * The pending bars of drawn rows that break a rule: a bar outside every work block of its row, or a bar that overlaps
 * another bar of the same row (half-open intervals).
 */
export function conflictsOf(rows: readonly ScheduleRow[], pendingIds: ReadonlySet<string>): Set<string> {
  const conflicts = new Set<string>();
  for (const row of rows) {
    for (const bar of row.bars.filter((candidate) => pendingIds.has(candidate.id))) {
      const isBroken =
        !isInsideOneBlock(bar, row.blocks) ||
        row.bars.some((other) => other.id !== bar.id && isOverlapping(bar, other));
      if (isBroken) {
        conflicts.add(bar.id);
      }
    }
  }
  return conflicts;
}

/** Apply needs 1 to 100 entries and no conflict. */
export function canApply(pending: readonly PendingChange[], conflictIds: ReadonlySet<string>): boolean {
  return pending.length >= 1 && pending.length <= SCHEDULE_APPLY_MAX_CHANGES && conflictIds.size === 0;
}

/**
 * The staging list: the loaded backlog without the work orders that a pending change puts in a slot, plus the work
 * orders that a pending change returns to the backlog.
 */
export function stagingOf(
  backlog: readonly StagingWorkOrder[],
  pending: readonly PendingChange[],
  known: ReadonlyMap<string, KnownWorkOrder>,
): StagingWorkOrder[] {
  const byId = new Map(pending.map((entry) => [entry.workOrderId, entry]));
  const remaining = backlog
    .filter((item) => byId.get(item.id)?.assigneeId == null)
    .map((item) => ({ ...item, estimateMinutes: byId.get(item.id)?.estimateMinutes ?? item.estimateMinutes }));
  const returned = pending
    .filter((entry) => entry.assigneeId === null && !backlog.some((item) => item.id === entry.workOrderId))
    .flatMap((entry) => {
      const base = known.get(entry.workOrderId);
      return base === undefined
        ? []
        : [{ id: base.id, title: base.title, priority: base.priority, estimateMinutes: entry.estimateMinutes }];
    });
  return [...returned, ...remaining];
}
