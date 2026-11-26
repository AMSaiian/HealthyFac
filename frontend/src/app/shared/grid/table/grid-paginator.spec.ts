import { type KeysetMove, toKeysetMove, toPaginator } from './grid-paginator';

describe('toKeysetMove', { tags: ['Persistence'] }, () => {
  it.each([
    ['OneStepForward', { page: 2, rows: 25, pageIndex: 1, limit: 25 }, 'next'],
    ['OneStepBack', { page: 0, rows: 25, pageIndex: 1, limit: 25 }, 'previous'],
    ['ANewPageSize', { page: 0, rows: 50, pageIndex: 3, limit: 25 }, 'limit'],
    ['ANewPageSizeOnAStep', { page: 4, rows: 50, pageIndex: 3, limit: 25 }, 'limit'],
    ['AJumpOfTwoPages', { page: 3, rows: 25, pageIndex: 1, limit: 25 }, 'none'],
    ['TheSamePage', { page: 1, rows: 25, pageIndex: 1, limit: 25 }, 'none'],
  ] as const)('toKeysetMove_With%s_Returns%s', (_name, event, expected: KeysetMove) => {
    expect(toKeysetMove(event)).toBe(expected);
  });
});

describe('toPaginator', { tags: ['Persistence'] }, () => {
  it('toPaginator_WithoutAPageIndex_KeepsTheOffsetDefaults', () => {
    expect(toPaginator(null, 25)).toEqual({
      first: null,
      showPageLinks: true,
      showFirstLastIcon: true,
      showJumpToPageDropdown: false,
    });
  });

  it('toPaginator_WithAPageIndex_HidesPageLinksAndKeepsTheReportOffset', () => {
    expect(toPaginator(3, 25)).toEqual({
      first: 75,
      showPageLinks: false,
      showFirstLastIcon: false,
      showJumpToPageDropdown: false,
    });
  });
});
