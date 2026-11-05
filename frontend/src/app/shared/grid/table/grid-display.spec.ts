import { MAX_SKELETON_ROWS, skeletonRows } from './grid-display';

describe('skeletonRows', () => {
  it('skeletonRows_WithLimitAboveMaximum_CapsRowCount', () => {
    const rows = skeletonRows(100);

    expect(rows).toHaveLength(MAX_SKELETON_ROWS);
  });

  it('skeletonRows_WithSmallLimit_ReturnsLimitRows', () => {
    const rows = skeletonRows(3);

    expect(rows).toEqual([0, 1, 2]);
  });
});
