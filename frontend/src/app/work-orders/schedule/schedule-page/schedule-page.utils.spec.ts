import { WorkOrderPriority } from '@work-orders/work-order';

import { backlogDrawerStateOf, isBacklogOpenAfterDrag, isReturnBandShown, stagingCardOf } from './schedule-page.utils';

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

describe('backlogDrawerStateOf', () => {
  it.each([
    ['OpenWithoutADrag', true, false, true, false],
    ['ClosedWithoutADrag', false, false, false, false],
    ['ClosedDuringABacklogDrag', false, true, true, true],
    ['OpenDuringABacklogDrag', true, true, true, false],
  ])('backlogDrawerStateOf_For%s_Decides', (_name, isOpen, isDragActive, isMounted, isHidden) => {
    expect(backlogDrawerStateOf(isOpen, isDragActive)).toEqual({ isMounted, isHidden });
  });
});

describe('isBacklogOpenAfterDrag', () => {
  it.each([
    ['BacklogDragStartedWhileOpen', true, 'backlogDragStarted', false],
    ['BoardDragStartedWhileOpen', true, 'boardDragStarted', false],
    ['BoardDragStartedWhileClosed', false, 'boardDragStarted', false],
    ['BoardDragEndedWhileOpen', true, 'boardDragEnded', true],
    ['BoardDragEndedWhileClosed', false, 'boardDragEnded', false],
  ] as const)('isBacklogOpenAfterDrag_For%s_Decides', (_name, isOpen, event, expected) => {
    expect(isBacklogOpenAfterDrag(isOpen, event)).toBe(expected);
  });
});

describe('isReturnBandShown', () => {
  it.each([
    ['ADragOfABoardBarByAManager', true, true, true],
    ['NoDrag', false, true, false],
    ['ADragWithoutManageRights', true, false, false],
  ])('isReturnBandShown_For%s_Decides', (_name, isDragActive, canManage, isShown) => {
    expect(isReturnBandShown(isDragActive, canManage)).toBe(isShown);
  });
});
