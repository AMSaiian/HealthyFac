import { type Axis, widthPercentOf } from '@shared/schedule/schedule-board/schedule-board.utils';

const MINUTES_PER_HOUR = 60;
const PERCENT = 100;
/** The narrowest drag preview in pixels. A very short estimate stays visible. */
const MIN_PREVIEW_PX = 8;

/** A backlog work order as the staging area shows it. The page maps the priority to a label key and a color class. */
export interface StagingItem {
  readonly id: string;
  readonly title: string;
  readonly estimateMinutes: number;
  /** The translation key of the priority label. */
  readonly priorityKey: string;
  /** The CSS class that colors the priority tag. */
  readonly priorityClass: string;
}

export interface EstimateText {
  readonly key: string;
  readonly params: { readonly hours: number; readonly minutes: number };
}

/** The estimate as a translation key with its parameters: minutes only, whole hours, or hours and minutes. */
export function estimateTextOf(estimateMinutes: number): EstimateText {
  const hours = Math.floor(estimateMinutes / MINUTES_PER_HOUR);
  const minutes = estimateMinutes % MINUTES_PER_HOUR;
  const key =
    hours === 0
      ? 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_MINUTES'
      : minutes === 0
        ? 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_HOURS'
        : 'WORK_ORDERS.SCHEDULE_TAB.ESTIMATE_HOURS_MINUTES';
  return { key, params: { hours, minutes } };
}

/** The width of the drag preview of a card in pixels: the estimate on the scale of the board axis. */
export function previewWidthOf(estimateMinutes: number, axis: Axis, axisWidthPx: number): number {
  return Math.max(MIN_PREVIEW_PX, (widthPercentOf(estimateMinutes, axis) / PERCENT) * axisWidthPx);
}

/** The tooltip of a chip: the full title on the first line, the estimate text on the second. */
export function chipTooltipOf(title: string, estimateText: string): string {
  return `${title}\n${estimateText}`;
}
