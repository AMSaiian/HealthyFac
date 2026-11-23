import { WorkOrderPriority } from '@work-orders/work-order';

import { BACKLOG_DRAWER_REM, CONTENT_GUTTER_REM, contentPaddingRemOf, stagingCardOf } from './schedule-page.utils';

describe('stagingCardOf', () => {
  it('stagingCardOf_ForAHighPriorityWorkOrder_GivesTheLabelKeyAndTheColorClass', () => {
    const card = stagingCardOf({ id: 'w1', title: 'Fix', estimateMinutes: 45, priority: WorkOrderPriority.High });

    expect(card).toEqual({
      id: 'w1',
      title: 'Fix',
      estimateMinutes: 45,
      priorityKey: 'WORK_ORDERS.PRIORITIES.HIGH',
      priorityClass: 'hf-priority-high',
    });
  });
});

describe('contentPaddingRemOf', () => {
  it.each([
    ['SupervisorWithAnOpenDrawer', true, true, BACKLOG_DRAWER_REM + CONTENT_GUTTER_REM],
    ['SupervisorWithAClosedDrawer', true, false, CONTENT_GUTTER_REM],
    ['WorkerWhoseDrawerFlagIsOpen', false, true, CONTENT_GUTTER_REM],
  ])('contentPaddingRemOf_For%s_KeepsTheDrawerWidthFreeOnlyWhenItShows', (_name, canManage, isOpen, expected) => {
    expect(contentPaddingRemOf(canManage, isOpen)).toBe(expected);
  });
});
