import type { OperatingPhase } from '@machines/phases/operating-phase';
import {
  movedOrder,
  sortByPriority,
} from '@machines/phases/operating-phases-page-store/operating-phases-page.store.utils';

function phase(id: string, priority: number): OperatingPhase {
  return {
    id,
    code: id,
    name: id,
    description: null,
    priority,
    isTracked: true,
    conditions: [],
    limits: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
  };
}

describe('operating phases page store utils', () => {
  it('sortByPriority_UnorderedPhases_PutsTheLowestPriorityFirstWithoutChangingTheInput', () => {
    const input = [phase('a', 1), phase('b', 5), phase('c', 3)];

    const result = sortByPriority(input);

    expect(result.map((item) => item.id)).toEqual(['a', 'c', 'b']);
    expect(input.map((item) => item.id)).toEqual(['a', 'b', 'c']);
  });

  it('movedOrder_RowMovedDown_PlacesTheIdAtTheTargetPosition', () => {
    expect(movedOrder([phase('a', 3), phase('b', 2), phase('c', 1)], 0, 2)).toEqual(['b', 'c', 'a']);
  });

  it('movedOrder_RowMovedUp_PlacesTheIdAtTheTargetPosition', () => {
    expect(movedOrder([phase('a', 3), phase('b', 2), phase('c', 1)], 2, 0)).toEqual(['c', 'a', 'b']);
  });

  it.each([
    [-1, 1],
    [0, 3],
    [1, 1],
  ])('movedOrder_PositionsOutsideTheListOrEqual_KeepsTheOrder_%i_%i', (from, to) => {
    expect(movedOrder([phase('a', 3), phase('b', 2), phase('c', 1)], from, to)).toEqual(['a', 'b', 'c']);
  });
});
