import {
  type Axis,
  LANE_GAP_REM,
  LANE_HEIGHT_REM,
  ROW_PADDING_REM,
  widthPercentOf,
} from '@shared/schedule/schedule-board/schedule-board.utils';

const FULL_WIDTH_PERCENT = 100;

export interface StagingItem {
  readonly id: string;
  readonly title: string;
  readonly estimateMinutes: number;
}

export interface PackedStagingItem {
  readonly item: StagingItem;
  readonly left: number;
  readonly width: number;
  readonly lane: number;
}

/**
 * Packs the staging bars from the left into lanes. A bar is as wide as its estimate on the axis scale (capped at the
 * lane width). A bar that no longer fits the rest of the lane starts the next lane.
 */
export function packLanes(
  items: readonly StagingItem[],
  axis: Axis,
): { packed: PackedStagingItem[]; laneCount: number } {
  const packed: PackedStagingItem[] = [];
  let lane = 0;
  let cursor = 0;
  for (const item of items) {
    const width = Math.min(FULL_WIDTH_PERCENT, widthPercentOf(item.estimateMinutes, axis));
    if (cursor > 0 && cursor + width > FULL_WIDTH_PERCENT) {
      lane += 1;
      cursor = 0;
    }
    packed.push({ item, left: cursor, width, lane });
    cursor += width;
  }
  return { packed, laneCount: Math.max(1, lane + 1) };
}

/** The top of a bar in the staging area in rem: the row padding and the lanes above it. */
export function stagingBarTopRem(lane: number): number {
  return ROW_PADDING_REM + lane * (LANE_HEIGHT_REM + LANE_GAP_REM);
}

/** The height of the lanes in rem. It keeps the area as tall as its bars. */
export function stagingHeightRem(laneCount: number): number {
  return laneCount * LANE_HEIGHT_REM + (laneCount - 1) * LANE_GAP_REM;
}
