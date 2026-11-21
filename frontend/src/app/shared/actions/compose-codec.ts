/** Tells whether the payload is an object whose keys all belong to `keys`, and whose present values are strings. */
export function hasOnlyStringKeys(payload: unknown, keys: readonly string[]): payload is Record<string, string> {
  return (
    typeof payload === 'object' &&
    payload !== null &&
    Object.entries(payload).every(([key, value]) => keys.includes(key) && typeof value === 'string')
  );
}
