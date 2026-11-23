import { axisOf } from '@shared/schedule/schedule-board/schedule-board.utils';
import { Temporal } from '@shared/time/temporal';

import { packLanes } from './staging-area.utils';

const AXIS = axisOf(Temporal.PlainDate.from('2027-06-01'), 'Europe/Kyiv');
const item = (id: string, estimateMinutes: number): { id: string; title: string; estimateMinutes: number } => ({
  id,
  title: id,
  estimateMinutes,
});

describe('packLanes', () => {
  it('packLanes_ForBarsThatFitOneLane_PlacesThemSideBySideFromTheLeft', () => {
    const { packed, laneCount } = packLanes([item('a', 360), item('b', 360)], AXIS);

    expect(packed.map((entry) => [entry.item.id, entry.lane, Math.round(entry.left)])).toEqual([
      ['a', 0, 0],
      ['b', 0, 25],
    ]);
    expect(laneCount).toBe(1);
  });

  it('packLanes_ForABarThatNoLongerFits_StartsTheNextLane', () => {
    const { packed, laneCount } = packLanes([item('a', 720), item('b', 720), item('c', 720)], AXIS);

    expect(packed.map((entry) => [entry.item.id, entry.lane, Math.round(entry.left)])).toEqual([
      ['a', 0, 0],
      ['b', 0, 50],
      ['c', 1, 0],
    ]);
    expect(laneCount).toBe(2);
  });

  it('packLanes_ForNoBars_HasOneEmptyLane', () => {
    expect(packLanes([], AXIS)).toEqual({ packed: [], laneCount: 1 });
  });
});
