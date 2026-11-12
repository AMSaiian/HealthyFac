/**
 * Tells whether the pending values of a multi-select differ from the applied values.
 * The order of the values does not count, so a reordered selection is not a change.
 */
export function isFilterChanged<TValue extends string>(
  applied: readonly TValue[],
  pending: readonly TValue[],
): boolean {
  const appliedValues = new Set(applied);
  const pendingValues = new Set(pending);

  return appliedValues.size !== pendingValues.size || [...pendingValues].some((value) => !appliedValues.has(value));
}

/**
 * Tells whether the value of one store filter field changed. Lists compare as sets. An empty value is a missing
 * value, a blank text, an empty list, or a range with open bounds: two empty values are the same.
 */
export function isFilterValueChanged(applied: unknown, pending: unknown): boolean {
  if (Array.isArray(applied) || Array.isArray(pending)) {
    return isFilterChanged(toStrings(applied), toStrings(pending));
  }
  if (isEmptyValue(applied) || isEmptyValue(pending)) {
    return !(isEmptyValue(applied) && isEmptyValue(pending));
  }

  return JSON.stringify(applied) !== JSON.stringify(pending);
}

function toStrings(value: unknown): string[] {
  return Array.isArray(value) ? (value as string[]) : [];
}

function isEmptyValue(value: unknown): boolean {
  if (value === null || value === undefined || value === '') {
    return true;
  }
  const isPlainObject = typeof value === 'object' && Object.getPrototypeOf(value) === Object.prototype;

  return isPlainObject && Object.values(value as object).every((field) => field === null || field === undefined);
}
