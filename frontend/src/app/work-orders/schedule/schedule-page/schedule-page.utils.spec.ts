import { WorkOrderPriority } from '@work-orders/work-order';

import {
  backlogDrawerStateOf,
  backlogPointerActionOf,
  isBacklogOpenAfterDrag,
  isReturnBandShown,
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
    ['OpenWithoutADrag', true, false, false, true, false],
    ['ClosedWithoutADrag', false, false, false, false, false],
    ['ClosedDuringABacklogDrag', false, true, false, true, true],
    ['OpenDuringABacklogDrag', true, true, false, true, false],
    ['ClosedAfterABacklogDrag', false, false, true, false, true],
    ['OpenedAfterABacklogDrag', true, false, true, true, false],
  ])('backlogDrawerStateOf_For%s_Decides', (_name, isOpen, isDragActive, isClosingQuietly, isMounted, isHidden) => {
    expect(backlogDrawerStateOf(isOpen, isDragActive, isClosingQuietly)).toEqual({ isMounted, isHidden });
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

describe('backlogPointerActionOf', () => {
  it.each([
    ['InsideTheDrawer', true, false, 'stay'],
    ['OnTheToggleButton', false, true, 'toggle'],
    ['OnTheBoard', false, false, 'close'],
  ] as const)('backlogPointerActionOf_For%s_Decides', (_name, isInsideDrawer, isOnToggle, expected) => {
    expect(backlogPointerActionOf(isInsideDrawer, isOnToggle)).toBe(expected);
  });
});
