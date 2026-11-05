export const EMPTY_CELL = '—';
export const MAX_SKELETON_ROWS = 10;

/** The placeholder rows of a loading grid: the page size, at most `MAX_SKELETON_ROWS`. */
export function skeletonRows(limit: number): number[] {
  return Array.from({ length: Math.min(limit, MAX_SKELETON_ROWS) }, (_, index) => index);
}
