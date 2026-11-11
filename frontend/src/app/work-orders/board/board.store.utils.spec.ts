import { SortDirection } from '@shared/grid/requests/grid-request';
import { Temporal } from '@shared/time/temporal';

import { type WorkOrderRow, WorkOrderStatus } from '@work-orders/work-order';
import { INITIAL_FILTER } from '@work-orders/list/work-orders-grid.store.utils';

import {
  BOARD_STATUSES,
  canEnterColumn,
  columnBody,
  dropDecision,
  slotsOf,
  statusTargets,
  touchedColumns,
  windowRequest,
} from './board.store.utils';

const TODAY = Temporal.PlainDate.from('2027-06-01');
const RANGE = { from: TODAY, to: TODAY };

describe('windowRequest', () => {
  it('windowRequest_WithSmallRange_AddsTheBuffer', () => {
    const request = windowRequest({ first: 20, last: 30 });

    expect(request).toEqual({ offset: 20, limit: 20 });
  });

  it('windowRequest_WithBigRange_CapsTheLimitAt100', () => {
    const request = windowRequest({ first: 0, last: 500 });

    expect(request).toEqual({ offset: 0, limit: 100 });
  });
});

describe('slotsOf', () => {
  it('slotsOf_WithWindowInTheMiddle_FillsOnlyTheLoadedPositions', () => {
    const row = { id: 'r1' } as WorkOrderRow;

    const slots = slotsOf({ offset: 2, items: [row], totalCount: 5 });

    expect(slots).toEqual([null, null, row, null, null]);
  });
});

describe('columnBody', () => {
  it('columnBody_WithScheduledStatus_FiltersTheStatusAndTheDateRange', () => {
    const body = columnBody(WorkOrderStatus.Staged, { first: 0, last: 10 }, INITIAL_FILTER, RANGE, 'Europe/Kyiv');

    expect(body.filter.statuses).toEqual({ values: ['staged'] });
    expect(body.filter.plannedStart).toEqual({ from: '2027-05-31T21:00:00Z', to: '2027-06-01T21:00:00Z' });
    expect(body.sort).toEqual({ column: 'plannedStart', direction: SortDirection.Ascending });
  });

  it('columnBody_ForBacklog_IgnoresTheDateRangeAndSortsByPriority', () => {
    const body = columnBody(WorkOrderStatus.Backlog, { first: 0, last: 10 }, INITIAL_FILTER, RANGE, 'Europe/Kyiv');

    expect(body.filter).not.toHaveProperty('plannedStart');
    expect(body.sort).toEqual({ column: 'priority', direction: SortDirection.Descending });
  });

  it('columnBody_WithSharedFilter_PassesTheTitleAndThePriorities', () => {
    const filter = { ...INITIAL_FILTER, title: 'pump', priorities: ['high' as never] };

    const body = columnBody(WorkOrderStatus.Blocked, { first: 0, last: 10 }, filter, RANGE, 'Europe/Kyiv');

    expect(body.filter.title).toEqual({ value: 'pump' });
    expect(body.filter.priorities).toEqual({ values: ['high'] });
  });
});

describe('dropDecision', () => {
  const S = WorkOrderStatus;

  it.each([
    [S.Staged, S.Staged, true, 'none'],
    [S.Backlog, S.Backlog, true, 'none'],
    [S.Staged, S.InProgress, true, 'status'],
    [S.Staged, S.InProgress, false, 'status'],
    [S.Blocked, S.Completed, false, 'status'],
    [S.InProgress, S.Rejected, true, 'status'],
    [S.Backlog, S.Staged, true, 'assign'],
    [S.Backlog, S.Staged, false, 'refused'],
    [S.Backlog, S.InProgress, true, 'refused'],
    [S.Backlog, S.Completed, true, 'refused'],
    [S.Staged, S.Backlog, true, 'unassign'],
    [S.Staged, S.Backlog, false, 'refused'],
    [S.Completed, S.Backlog, true, 'unassign'],
    [S.Rejected, S.Backlog, false, 'refused'],
  ])('dropDecision_From%sTo%sWithManage%s_Gives%s', (source, target, canManage, expected) => {
    const decision = dropDecision(source, target, canManage);

    expect(decision).toBe(expected);
  });
});

describe('touchedColumns', () => {
  it('touchedColumns_ForAStatusDrop_AreTheSourceAndTheTarget', () => {
    const columns = touchedColumns(WorkOrderStatus.Staged, WorkOrderStatus.Blocked);

    expect(columns).toEqual([WorkOrderStatus.Staged, WorkOrderStatus.Blocked]);
  });

  it('touchedColumns_ForACompletion_AreEveryColumn', () => {
    const columns = touchedColumns(WorkOrderStatus.Staged, WorkOrderStatus.Completed);

    expect(columns).toEqual(BOARD_STATUSES);
  });
});

describe('statusTargets', () => {
  it('statusTargets_ForAWorkerOnAScheduledCard_LeaveOutTheBacklog', () => {
    const targets = statusTargets(WorkOrderStatus.Staged, false);

    expect(targets).toEqual([
      WorkOrderStatus.InProgress,
      WorkOrderStatus.Blocked,
      WorkOrderStatus.Completed,
      WorkOrderStatus.Rejected,
    ]);
  });

  it('statusTargets_ForASupervisorOnABacklogCard_OnlyStaged', () => {
    const targets = statusTargets(WorkOrderStatus.Backlog, true);

    expect(targets).toEqual([WorkOrderStatus.Staged]);
  });
});

describe('canEnterColumn', () => {
  it('canEnterColumn_WhileTheColumnLoads_RefusesTheCard', () => {
    const result = canEnterColumn(WorkOrderStatus.Staged, WorkOrderStatus.Blocked, true, true);

    expect(result).toBe(false);
  });

  it('canEnterColumn_WhenTheColumnIsReady_FollowsTheDropTable', () => {
    const allowed = canEnterColumn(WorkOrderStatus.Staged, WorkOrderStatus.Blocked, false, false);
    const refused = canEnterColumn(WorkOrderStatus.Backlog, WorkOrderStatus.Blocked, true, false);

    expect(allowed).toBe(true);
    expect(refused).toBe(false);
  });
});
