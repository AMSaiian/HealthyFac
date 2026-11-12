import { isEmptyResult, MAX_SKELETON_ROWS, skeletonRows } from './grid-display';

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

describe('isEmptyResult', () => {
  it.each([
    ['NotLoadedYet', { rows: [], isLoading: false, hasLoaded: false }, false],
    ['Loading', { rows: [], isLoading: true, hasLoaded: true }, false],
    ['LoadedWithRows', { rows: [{ id: 'a' }], isLoading: false, hasLoaded: true }, false],
    ['LoadedWithoutRows', { rows: [], isLoading: false, hasLoaded: true }, true],
  ])('isEmptyResult_%s_ReturnsExpected', (_name, state, expected) => {
    const result = isEmptyResult(state);

    expect(result).toBe(expected);
  });
});
