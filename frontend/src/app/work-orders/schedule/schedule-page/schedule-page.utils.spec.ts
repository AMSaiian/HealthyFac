import { WorkOrderPriority } from '@work-orders/work-order';

import {
  BACKLOG_BUTTON_TEXT_KEY,
  BACKLOG_DROP_TEXT_KEY,
  backlogButtonOf,
  backlogDrawerStateOf,
  isBacklogOpenAfterDrag,
  stagingCardOf,
} from './schedule-page.utils';

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

describe('backlogButtonOf', () => {
  it('backlogButtonOf_DuringADragOfAnAssignedBar_ShowsTheDropTextAndTheHighlight', () => {
    expect(backlogButtonOf(true)).toEqual({ textKey: BACKLOG_DROP_TEXT_KEY, isHighlighted: true });
  });

  it('backlogButtonOf_WithoutADrag_ShowsTheCountTextWithoutTheHighlight', () => {
    expect(backlogButtonOf(false)).toEqual({ textKey: BACKLOG_BUTTON_TEXT_KEY, isHighlighted: false });
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
