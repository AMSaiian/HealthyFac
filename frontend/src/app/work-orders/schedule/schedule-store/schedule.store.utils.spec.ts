import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import type { ScheduleBar, ScheduleRow } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import { WorkOrderStatus } from '@work-orders/work-order';
import {
  applyPending,
  canApply,
  conflictsOf,
  isChanged,
  type KnownWorkOrder,
  localStartOf,
  type PendingChange,
  stagingOf,
  withTarget,
} from './schedule.store.utils';

const ZONE = 'Europe/Kyiv';
const DATE = Temporal.PlainDate.from('2027-06-01');
const AXIS = axisOf(DATE, ZONE);

function at(time: string): Temporal.Instant {
  return Temporal.PlainDateTime.from(`2027-06-01T${time}`).toZonedDateTime(ZONE).toInstant();
}

function bar(id: string, from: string, to: string): ScheduleBar {
  return { id, title: id, start: at(from), end: at(to), statusKey: 'S', colorClass: 'hf-status-staged' };
}

function known(id: string, assigneeId: string | null, start: string | null, estimate = 60): KnownWorkOrder {
  return {
    id,
    title: id,
    status: assigneeId === null ? WorkOrderStatus.Backlog : WorkOrderStatus.Staged,
    rowVersion: 3,
    estimateMinutes: estimate,
    assigneeId,
    plannedStartLocal: start === null ? null : `2027-06-01T${start}`,
  };
}

function row(id: string, bars: ScheduleBar[]): ScheduleRow {
  return { id, label: id, blocks: [{ start: at('08:00:00'), end: at('12:00:00') }], bars };
}

describe('schedule store utils', () => {
  describe('withTarget', () => {
    const first = known('w1', 'e1', '09:00:00');

    it('withTarget_ForAMove_AddsOneEntryWithTheRowVersionOfTheServer', () => {
      const result = withTarget([], first, {
        assigneeId: 'e2',
        plannedStartLocal: '2027-06-01T10:00:00',
        estimateMinutes: 60,
      });

      expect(result).toEqual([
        {
          workOrderId: 'w1',
          rowVersion: 3,
          assigneeId: 'e2',
          plannedStartLocal: '2027-06-01T10:00:00',
          estimateMinutes: 60,
        },
      ]);
    });

    it('withTarget_ForAMoveBackToTheServerState_RemovesTheEntry', () => {
      const moved = withTarget([], first, {
        assigneeId: 'e2',
        plannedStartLocal: '2027-06-01T10:00:00',
        estimateMinutes: 60,
      });

      const result = withTarget(moved, first, {
        assigneeId: 'e1',
        plannedStartLocal: '2027-06-01T09:00',
        estimateMinutes: 60,
      });

      expect(result).toEqual([]);
    });

    it('withTarget_ForASecondChangeOfOneWorkOrder_KeepsOneEntryInTheSamePlace', () => {
      const other = known('w2', 'e1', '11:00:00');
      const both = withTarget(
        withTarget([], first, { assigneeId: 'e1', plannedStartLocal: '2027-06-01T09:00:00', estimateMinutes: 90 }),
        other,
        { assigneeId: 'e1', plannedStartLocal: '2027-06-01T11:00:00', estimateMinutes: 30 },
      );

      const result = withTarget(both, first, {
        assigneeId: 'e1',
        plannedStartLocal: '2027-06-01T09:00:00',
        estimateMinutes: 120,
      });

      expect(result.map((entry) => [entry.workOrderId, entry.estimateMinutes])).toEqual([
        ['w1', 120],
        ['w2', 30],
      ]);
    });

    it('isChanged_ForTheSameStartInTwoTextForms_IsFalse', () => {
      const result = isChanged(first, { assigneeId: 'e1', plannedStartLocal: '2027-06-01T09:00', estimateMinutes: 60 });

      expect(result).toBe(false);
    });
  });

  describe('localStartOf', () => {
    it.each([
      ['2027-06-01T06:00:00Z', '2027-06-01T09:00:00'],
      ['2027-01-15T07:30:00Z', '2027-01-15T09:30:00'],
    ])('localStartOf_Of%s_Is%s', (instant, expected) => {
      expect(localStartOf(Temporal.Instant.from(instant), ZONE)).toBe(expected);
    });
  });

  describe('applyPending', () => {
    const rows = [row('e1', [bar('w1', '09:00', '10:00')]), row('e2', [])];
    const knownMap = new Map([
      ['w1', known('w1', 'e1', '09:00:00')],
      ['w3', known('w3', null, null)],
    ]);

    it('applyPending_ForAMoveToAnotherRow_MovesTheBar', () => {
      const pending: PendingChange[] = [
        {
          workOrderId: 'w1',
          rowVersion: 3,
          assigneeId: 'e2',
          plannedStartLocal: '2027-06-01T10:00:00',
          estimateMinutes: 60,
        },
      ];

      const result = applyPending(rows, pending, knownMap, AXIS, ZONE);

      expect(result[0].bars).toEqual([]);
      expect(result[1].bars.map((item) => [item.id, item.start.toString()])).toEqual([['w1', at('10:00').toString()]]);
    });

    it('applyPending_ForABacklogWorkOrderFromStaging_DrawsAStagedBar', () => {
      const pending: PendingChange[] = [
        {
          workOrderId: 'w3',
          rowVersion: 3,
          assigneeId: 'e2',
          plannedStartLocal: '2027-06-01T08:00:00',
          estimateMinutes: 30,
        },
      ];

      const result = applyPending(rows, pending, knownMap, AXIS, ZONE);

      expect(result[1].bars.map((item) => [item.id, item.colorClass, item.end.toString()])).toEqual([
        ['w3', 'hf-status-staged', at('08:30').toString()],
      ]);
    });

    it('applyPending_ForAnUnassign_RemovesTheBarFromEveryRow', () => {
      const pending: PendingChange[] = [
        { workOrderId: 'w1', rowVersion: 3, assigneeId: null, plannedStartLocal: null, estimateMinutes: 60 },
      ];

      const result = applyPending(rows, pending, knownMap, AXIS, ZONE);

      expect(result.flatMap((item) => item.bars)).toEqual([]);
    });

    it('applyPending_ForAStartOnAnotherDay_DrawsNoBarOnThisDay', () => {
      const pending: PendingChange[] = [
        {
          workOrderId: 'w1',
          rowVersion: 3,
          assigneeId: 'e1',
          plannedStartLocal: '2027-06-02T09:00:00',
          estimateMinutes: 60,
        },
      ];

      const result = applyPending(rows, pending, knownMap, AXIS, ZONE);

      expect(result.flatMap((item) => item.bars)).toEqual([]);
    });
  });

  describe('conflictsOf', () => {
    it('conflictsOf_ForABarOutsideEveryBlock_IsAConflict', () => {
      const rows = [row('e1', [bar('w1', '12:00', '13:00')])];

      expect([...conflictsOf(rows, new Set(['w1']))]).toEqual(['w1']);
    });

    it('conflictsOf_ForABarThatReachesPastTheBlockEnd_IsAConflict', () => {
      const rows = [row('e1', [bar('w1', '11:30', '12:30')])];

      expect([...conflictsOf(rows, new Set(['w1']))]).toEqual(['w1']);
    });

    it('conflictsOf_ForAnOverlapWithAnotherBar_IsAConflict', () => {
      const rows = [row('e1', [bar('w1', '09:00', '10:00'), bar('w2', '09:30', '10:30')])];

      expect([...conflictsOf(rows, new Set(['w2']))]).toEqual(['w2']);
    });

    it('conflictsOf_ForBarsThatTouchAtTheBoundary_IsNoConflict', () => {
      const rows = [row('e1', [bar('w1', '09:00', '10:00'), bar('w2', '10:00', '11:00')])];

      expect([...conflictsOf(rows, new Set(['w1', 'w2']))]).toEqual([]);
    });

    it('conflictsOf_ForAnUnchangedBarOutsideTheBlock_IsNoConflict', () => {
      const rows = [row('e1', [bar('w1', '13:00', '14:00')])];

      expect([...conflictsOf(rows, new Set())]).toEqual([]);
    });
  });

  describe('canApply', () => {
    const entry = (index: number): PendingChange => ({
      workOrderId: `w${index}`,
      rowVersion: 1,
      assigneeId: 'e1',
      plannedStartLocal: '2027-06-01T09:00:00',
      estimateMinutes: 60,
    });

    it.each([
      ['NoEntries', 0, [], false],
      ['OneEntry', 1, [], true],
      ['100Entries', 100, [], true],
      ['101Entries', 101, [], false],
      ['OneConflict', 2, ['w0'], false],
    ])('canApply_With%s_Is%s', (_name, count, conflicts, expected) => {
      const pending = Array.from({ length: count }, (_, index) => entry(index));

      expect(canApply(pending, new Set(conflicts))).toBe(expected);
    });
  });

  describe('stagingOf', () => {
    const backlog = [
      { id: 'w3', title: 'w3', estimateMinutes: 60 },
      { id: 'w4', title: 'w4', estimateMinutes: 30 },
    ];
    const knownMap = new Map([['w1', known('w1', 'e1', '09:00:00')]]);

    it('stagingOf_ForAStagedAssign_LeavesTheWorkOrderOutOfTheList', () => {
      const pending: PendingChange[] = [
        {
          workOrderId: 'w3',
          rowVersion: 3,
          assigneeId: 'e1',
          plannedStartLocal: '2027-06-01T09:00:00',
          estimateMinutes: 60,
        },
      ];

      expect(stagingOf(backlog, pending, knownMap).map((item) => item.id)).toEqual(['w4']);
    });

    it('stagingOf_ForAnUnassign_PutsTheWorkOrderFirstInTheList', () => {
      const pending: PendingChange[] = [
        { workOrderId: 'w1', rowVersion: 3, assigneeId: null, plannedStartLocal: null, estimateMinutes: 45 },
      ];

      expect(stagingOf(backlog, pending, knownMap).map((item) => [item.id, item.estimateMinutes])).toEqual([
        ['w1', 45],
        ['w3', 60],
        ['w4', 30],
      ]);
    });
  });
});
