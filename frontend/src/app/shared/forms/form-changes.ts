import { createCustomEqual } from 'fast-equals';
import type { ZodMiniType } from 'zod/mini';

const TEMPORAL_TAG_PREFIX = '[object Temporal.';

interface TemporalValue {
  equals(other: unknown): boolean;
}

/**
 * A structural deep compare of plain data. A Temporal value has no own keys, so the default compare treats two
 * different values as equal. The `equals` method of the value compares them instead.
 */
const formEqual = createCustomEqual({
  createCustomConfig: () => ({
    getUnsupportedCustomComparator: (_a, _b, _state, tag) =>
      tag.startsWith(TEMPORAL_TAG_PREFIX) ? (a: TemporalValue, b: unknown) => a.equals(b) : undefined,
  }),
});

/**
 * Tells whether the form value differs from its start value. Both values pass through the schema, so its
 * normalization (e.g., `trim()`) hides changes that have no meaning. An invalid value counts as a change: the
 * invalid reason disables **Save** first anyway. The comparison is a structural deep compare of plain data and
 * Temporal values.
 */
export function hasFormChanges<T>(schema: ZodMiniType<T>, initial: T, current: T): boolean {
  const parsedInitial = schema.safeParse(initial);
  const parsedCurrent = schema.safeParse(current);
  if (!parsedInitial.success || !parsedCurrent.success) {
    return true;
  }
  return !formEqual(parsedInitial.data, parsedCurrent.data);
}
