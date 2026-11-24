import { Temporal } from '@shared/time/temporal';

import { GRID_STEP_MINUTES } from '@work-orders/work-order-limits';
import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';

const WEEKS_SHOWN = 6;
const DAYS_PER_WEEK = 7;
const MINUTES_PER_HOUR = 60;
const LAST_HOUR = 23;
const MS_PER_MINUTE = 60_000;

/** How a day looks to the picker for the estimate: the label of its calendar cell. */
export enum DayLabel {
  Fits = 'fits',
  TooShort = 'tooShort',
  Busy = 'busy',
  DayOff = 'dayOff',
}

/** Whether a free span holds the estimate. */
export enum SpanState {
  Fits = 'fits',
  TooShort = 'tooShort',
}

/** The earliest and the latest start inside a span, as times of the day on the grid. */
export interface StartBounds {
  readonly earliest: Temporal.PlainTime;
  readonly latest: Temporal.PlainTime;
}

/** The first and the last date that the panel of a month shows. */
export interface ShownRange {
  readonly from: Temporal.PlainDate;
  readonly to: Temporal.PlainDate;
}

export function dayLabelOf(day: ScheduleDay, estimateMinutes: number): DayLabel {
  if (!day.hasBlocks) {
    return DayLabel.DayOff;
  }
  if (day.longestFreeMinutes === 0) {
    return DayLabel.Busy;
  }
  return day.longestFreeMinutes >= estimateMinutes ? DayLabel.Fits : DayLabel.TooShort;
}

/**
 * Gives the dates that the panel of `month` can show: 6 weeks from the first weekday on or before the first day of the
 * month. `firstDayOfWeek` is 0 for Sunday and 1 for Monday, as the PrimeNG locale gives it.
 */
export function shownRange(month: Temporal.PlainYearMonth, firstDayOfWeek: number): ShownRange {
  const first = month.toPlainDate({ day: 1 });
  const weekday = first.dayOfWeek % DAYS_PER_WEEK;
  const offset = (weekday - firstDayOfWeek + DAYS_PER_WEEK) % DAYS_PER_WEEK;
  const from = first.subtract({ days: offset });

  return { from, to: from.add({ days: WEEKS_SHOWN * DAYS_PER_WEEK - 1 }) };
}

/** The real length of a span by its instants, in minutes. */
export function spanMinutes(span: FreeSpan): number {
  return (
    (Temporal.Instant.from(span.endUtc).epochMilliseconds - Temporal.Instant.from(span.startUtc).epochMilliseconds) /
    MS_PER_MINUTE
  );
}

export function spanStateOf(span: FreeSpan, estimateMinutes: number): SpanState {
  return spanMinutes(span) >= estimateMinutes ? SpanState.Fits : SpanState.TooShort;
}

function roundDownToGrid(time: Temporal.PlainTime): Temporal.PlainTime {
  return time.with({ minute: time.minute - (time.minute % GRID_STEP_MINUTES), second: 0, millisecond: 0 });
}

/**
 * The starts that a span allows: from its start to its end minus the estimate. Returns `null` for a span that is
 * shorter than the estimate. A span that runs past midnight allows starts to the last grid step of its first day.
 */
export function startBounds(span: FreeSpan, estimateMinutes: number): StartBounds | null {
  if (spanStateOf(span, estimateMinutes) === SpanState.TooShort) {
    return null;
  }

  const start = Temporal.PlainDateTime.from(span.startLocal);
  const latest = Temporal.PlainDateTime.from(span.endLocal).subtract({ minutes: estimateMinutes });
  const lastGridStep = Temporal.PlainTime.from({ hour: LAST_HOUR, minute: MINUTES_PER_HOUR - GRID_STEP_MINUTES });

  return {
    earliest: start.toPlainTime(),
    latest:
      Temporal.PlainDate.compare(latest.toPlainDate(), start.toPlainDate()) > 0
        ? lastGridStep
        : roundDownToGrid(latest.toPlainTime()),
  };
}

/** The value of the picker: the person and the planned start, both `null` for no schedule. */
export interface AssignmentValue {
  readonly assigneeId: string | null;
  readonly start: Temporal.PlainDateTime | null;
}

/** The day of a calendar cell as PrimeNG gives it: the month is 0-based. */
export interface PickerCell {
  readonly year: number;
  readonly month: number;
  readonly day: number;
}

/** Gives the ISO date of a PrimeNG calendar cell, the key of the label map. */
export function cellKeyOf(cell: PickerCell): string {
  return Temporal.PlainDate.from({ year: cell.year, month: cell.month + 1, day: cell.day }).toString();
}

/** The class of a day label. The classes sit in `styles.css`. */
export function dayClassOf(label: DayLabel): string {
  return `hf-day-${label}`;
}

/**
 * Tells whether `start` lies before the first span of the day, or the day has no span. A free span never starts before
 * now, so a start that passed (a running work order) is before the first span. The backend judges such a start.
 */
export function isBeforeFirstSpan(start: Temporal.PlainDateTime, spans: readonly FreeSpan[]): boolean {
  return spans.every((span) => Temporal.PlainDateTime.compare(start, Temporal.PlainDateTime.from(span.startLocal)) < 0);
}

/** Tells whether `start` lies inside a span that holds the estimate: from the span start to its end minus the estimate. */
export function isStartAllowed(
  start: Temporal.PlainDateTime,
  spans: readonly FreeSpan[],
  estimateMinutes: number,
): boolean {
  return spans.some((span) => {
    if (spanStateOf(span, estimateMinutes) === SpanState.TooShort) {
      return false;
    }
    const earliest = Temporal.PlainDateTime.from(span.startLocal);
    const latest = Temporal.PlainDateTime.from(span.endLocal).subtract({ minutes: estimateMinutes });

    return Temporal.PlainDateTime.compare(start, earliest) >= 0 && Temporal.PlainDateTime.compare(start, latest) <= 0;
  });
}
