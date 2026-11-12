import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { Temporal } from '@shared/time/temporal';

import {
  type AffectedWorkOrder,
  type CreateVersionBody,
  type ShiftBlock,
  type ShiftDay,
  type ShiftOverride,
  type ShiftVersion,
  Weekday,
} from '@employees/shifts/shift';
import type { OverrideFormValue } from '@employees/shifts/override-dialog/override-dialog-schema';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';

export type ShiftDialog =
  | { readonly kind: 'version'; readonly version: ShiftVersion | null }
  | { readonly kind: 'override'; readonly override: ShiftOverride | null };

const TIME_LENGTH = 5;
const WORK_ORDERS_KEY = 'workOrders';

export const WEEKDAYS: readonly Weekday[] = Object.values(Weekday);

/** The translation key of each weekday. Literal keys, so the translation check finds each one. */
export const WEEKDAY_KEYS: Readonly<Record<Weekday, string>> = {
  [Weekday.Monday]: 'SHIFTS.WEEKDAYS.MONDAY',
  [Weekday.Tuesday]: 'SHIFTS.WEEKDAYS.TUESDAY',
  [Weekday.Wednesday]: 'SHIFTS.WEEKDAYS.WEDNESDAY',
  [Weekday.Thursday]: 'SHIFTS.WEEKDAYS.THURSDAY',
  [Weekday.Friday]: 'SHIFTS.WEEKDAYS.FRIDAY',
  [Weekday.Saturday]: 'SHIFTS.WEEKDAYS.SATURDAY',
  [Weekday.Sunday]: 'SHIFTS.WEEKDAYS.SUNDAY',
};

/** Cuts the seconds of a time (`08:00:00` becomes `08:00`). */
export function cutSeconds(time: string): string {
  return time.slice(0, TIME_LENGTH);
}

export function normalizeBlocks(blocks: readonly ShiftBlock[]): ShiftBlock[] {
  return blocks.map((block) => ({ start: cutSeconds(block.start), end: cutSeconds(block.end) }));
}

/** Gives a version one entry for each weekday in the week order, with the seconds of the times cut. */
export function normalizeVersion(version: ShiftVersion): ShiftVersion {
  const days: ShiftDay[] = WEEKDAYS.map((weekday) => ({
    weekday,
    blocks: normalizeBlocks(version.days.find((day) => day.weekday === weekday)?.blocks ?? []),
  }));
  return { ...version, days };
}

export function emptyDays(): { weekday: Weekday; blocks: ShiftBlock[] }[] {
  return WEEKDAYS.map((weekday) => ({ weekday, blocks: [] }));
}

export function toVersionFormValue(version: ShiftVersion | null, today: Temporal.PlainDate): VersionFormValue {
  return version === null
    ? { effectiveFrom: today.add({ days: 1 }), days: emptyDays() }
    : {
        effectiveFrom: Temporal.PlainDate.from(version.effectiveFrom),
        days: version.days.map((day) => ({ weekday: day.weekday, blocks: [...day.blocks] })),
      };
}

export function toCreateBody(value: VersionFormValue): CreateVersionBody {
  return {
    effectiveFrom: value.effectiveFrom?.toString() ?? '',
    days: value.days.map((day) => ({ weekday: day.weekday, blocks: day.blocks.map((block) => ({ ...block })) })),
  };
}

export function toOverrideBlocks(value: OverrideFormValue): ShiftBlock[] {
  return value.blocks.map((block) => ({ start: block.start, end: block.end }));
}

/** Reads the work orders of a `422` answer (`workOrders`). Returns `null` when the answer has none. */
export function affectedOf(error: unknown): AffectedWorkOrder[] | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.UnprocessableEntity) {
    return null;
  }
  const body: unknown = error.error;
  const list = typeof body === 'object' && body !== null ? (body as Record<string, unknown>)[WORK_ORDERS_KEY] : null;
  return Array.isArray(list) ? (list as AffectedWorkOrder[]) : null;
}

/** The first and the last day of the month of `month`, as `YYYY-MM-DD`. */
export function monthRange(month: Temporal.PlainDate): { from: string; to: string } {
  return { from: month.with({ day: 1 }).toString(), to: month.with({ day: month.daysInMonth }).toString() };
}

/** A version may change only before it takes effect: after today. */
export function isEditableVersion(version: ShiftVersion, today: Temporal.PlainDate): boolean {
  return version.effectiveFrom > today.toString();
}

/** The blocks of a day as text, e.g. `08:00-12:00, 13:00-17:00`. */
export function summaryOf(blocks: readonly ShiftBlock[]): string {
  return blocks.map((block) => `${block.start}-${block.end}`).join(', ');
}
