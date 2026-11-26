import { type ActivatedRouteSnapshot, type Route, UrlSegment, UrlSegmentGroup } from '@angular/router';

import { isOtherParamChanged, tabLink, tabMatcher } from './tab-route';

const TABS = ['summary', 'signals', 'aiLog'] as const;

function match(path: string, url: string): Record<string, string> | null {
  const segments = url === '' ? [] : url.split('/').map((part) => new UrlSegment(part, {}));
  const result = tabMatcher(path, TABS)(segments, new UrlSegmentGroup(segments, {}), {} as Route);
  if (result === null) {
    return null;
  }
  return Object.fromEntries(Object.entries(result.posParams ?? {}).map(([key, segment]) => [key, segment.path]));
}

function snapshot(params: Record<string, string>): ActivatedRouteSnapshot {
  return { params } as unknown as ActivatedRouteSnapshot;
}

describe('tab-route', { tags: ['Platform'] }, () => {
  it.each([
    [':caseId', 'c1', { caseId: 'c1' }],
    [':caseId', 'c1/signals', { caseId: 'c1', tab: 'signals' }],
    [':machineId/signals', 'm1/signals/aiLog', { machineId: 'm1', tab: 'aiLog' }],
    ['', '', {}],
    ['', 'signals', { tab: 'signals' }],
  ])('tabMatcher_With%sAnd%s_MatchesItsParams', (path, url, expected) => {
    expect(match(path, url)).toEqual(expected);
  });

  it.each([
    [':caseId', 'c1/summary'],
    [':caseId', 'c1/unknown'],
    [':caseId', 'c1/signals/extra'],
    [':machineId/signals', 'm1/files'],
    ['', 'e1/shifts'],
  ])('tabMatcher_With%sAnd%s_DoesNotMatch', (path, url) => {
    expect(match(path, url)).toBeNull();
  });

  it.each([
    [{ caseId: 'c1' }, { caseId: 'c1', tab: 'signals' }, false],
    [{ caseId: 'c1', tab: 'signals' }, { caseId: 'c1' }, false],
    [{ caseId: 'c1' }, { caseId: 'c2' }, true],
  ])('isOtherParamChanged_From%jTo%j_Is%s', (from, to, expected) => {
    expect(isOtherParamChanged(snapshot(from), snapshot(to))).toBe(expected);
  });

  it('tabLink_WithTheFirstAndALaterTab_AddsOnlyTheLaterTab', () => {
    expect(tabLink(['/maintenance', 'cases', 'c1'], TABS, 'summary')).toEqual(['/maintenance', 'cases', 'c1']);
    expect(tabLink(['/maintenance', 'cases', 'c1'], TABS, 'signals')).toEqual([
      '/maintenance',
      'cases',
      'c1',
      'signals',
    ]);
  });
});
