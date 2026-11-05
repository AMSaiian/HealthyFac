import type { ZodMiniType } from 'zod/mini';
import equal from 'fast-deep-equal/es6';

/**
 * Tells whether the form value differs from its start value. Both values pass through the schema, so its
 * normalization (e.g., `trim()`) hides changes that have no meaning. An invalid value counts as a change: the
 * invalid reason disables **Save** first anyway. The comparison uses `JSON.stringify`; both outputs follow the key
 * order of the schema, and form values are plain data.
 */
export function hasFormChanges<T>(schema: ZodMiniType<T>, initial: T, current: T): boolean {
  const parsedInitial = schema.safeParse(initial);
  const parsedCurrent = schema.safeParse(current);
  if (!parsedInitial.success || !parsedCurrent.success) {
    return true;
  }
  return !equal(parsedInitial.data, parsedCurrent.data);
}
