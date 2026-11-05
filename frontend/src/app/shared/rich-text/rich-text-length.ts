/** Counts the characters of the stored HTML string, which is the length that the backend limits. */
export function richTextLength(value: string | null): number {
  return value?.length ?? 0;
}
