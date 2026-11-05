import { EMPTY_RANGE, isRange, type PlainDateRange } from '@shared/time/plain-date-range';

const FILTER_MATCH_MODE = 'equals';
const FILTER_OPERATOR = 'and';

/** One constraint of a PrimeNG column filter. The grid uses one constraint per column. */
export interface TableConstraint {
  readonly value: unknown;
  readonly matchMode: string;
  readonly operator: string;
}

export type TableFilters = Record<string, TableConstraint[]>;

export const asText = (value: unknown): string => (typeof value === 'string' ? value.trim() : '');
export const asList = <T>(value: unknown): T[] => (Array.isArray(value) ? (value as T[]) : []);
export const asRange = (value: unknown): PlainDateRange => (isRange(value) ? value : EMPTY_RANGE);

export function toConstraint(value: unknown): TableConstraint {
  return { value, matchMode: FILTER_MATCH_MODE, operator: FILTER_OPERATOR };
}

export function constraintValue(entry: unknown): unknown {
  const constraint = Array.isArray(entry) ? entry[0] : entry;
  return (constraint as { value?: unknown } | undefined)?.value;
}

/**
 * Writes the applied store filter as the `[filters]` value of the table: one constraint per column. A `null` value
 * means "no filter".
 */
export function toTableFilters<TColumn extends string, TFilter>(
  columns: readonly TColumn[],
  writers: Readonly<Record<TColumn, (filter: TFilter) => unknown>>,
  filter: TFilter,
): TableFilters {
  return Object.fromEntries(columns.map((column) => [column, [toConstraint(writers[column](filter))]]));
}

/** Reads the pending values of the table filters as one store patch. A missing value means "empty". */
export function readTableFilters<TColumn extends string, TFilter>(
  columns: readonly TColumn[],
  readers: Readonly<Record<TColumn, (value: unknown) => Partial<TFilter>>>,
  filters: Readonly<Record<string, unknown>> | undefined,
): Partial<TFilter> {
  return Object.assign({}, ...columns.map((column) => readers[column](constraintValue(filters?.[column]))));
}
