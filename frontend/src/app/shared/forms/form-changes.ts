import type { ZodMiniType } from 'zod/mini';
import { deepEqual } from 'fast-equals';

/**
 * Tells whether the form value differs from its start value. Both values pass through the schema, so its
 * normalization (e.g., `trim()`) hides changes that have no meaning. An invalid value counts as a change: the
 * invalid reason disables **Save** first anyway. The comparison is a structural deep compare of plain data. A Temporal
 * value is not supported: the compare reports a change.
 */
export function hasFormChanges<T>(schema: ZodMiniType<T>, initial: T, current: T): boolean {
  const parsedInitial = schema.safeParse(initial);
  const parsedCurrent = schema.safeParse(current);
  if (!parsedInitial.success || !parsedCurrent.success) {
    return true;
  }
  return !deepEqual(parsedInitial.data, parsedCurrent.data);
}
