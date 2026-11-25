import type { ActivatedRouteSnapshot, UrlMatcher, UrlSegment } from '@angular/router';

/** The route param that holds the active tab of a page. */
export const TAB_PARAM = 'tab';

/**
 * Matches the whole `path` (fixed and `:param` segments) with an optional last segment that names a tab, like the
 * route segments of the work order views. The first tab has no segment, so the address without a tab stays valid. A
 * later tab puts its segment into the `tab` route param; an unknown segment does not match.
 */
export function tabMatcher(path: string, tabs: readonly [string, ...string[]]): UrlMatcher {
  const parts = path === '' ? [] : path.split('/');
  const segmentTabs = tabs.slice(1);

  return (segments) => {
    const tabCount = segments.length - parts.length;
    if (tabCount !== 0 && tabCount !== 1) {
      return null;
    }
    const posParams: Record<string, UrlSegment> = {};
    for (const [index, part] of parts.entries()) {
      const segment = segments[index];
      if (part.startsWith(':')) {
        posParams[part.slice(1)] = segment;
      } else if (part !== segment.path) {
        return null;
      }
    }
    if (tabCount === 1) {
      const tab = segments[parts.length];
      if (!segmentTabs.includes(tab.path)) {
        return null;
      }
      posParams[TAB_PARAM] = tab;
    }
    return { consumed: segments, posParams };
  };
}

/**
 * Runs the guards and the resolvers of a tab route again only when a param other than the tab changes, so a tab click
 * keeps the page and its store and loads nothing again.
 */
export function isOtherParamChanged(from: ActivatedRouteSnapshot, to: ActivatedRouteSnapshot): boolean {
  return Object.keys({ ...from.params, ...to.params }).some(
    (key) => key !== TAB_PARAM && from.params[key] !== to.params[key],
  );
}

/** Returns the link commands of a tab: the page address for the first tab, else the page address and the tab. */
export function tabLink(page: readonly string[], tabs: readonly [string, ...string[]], tab: string): string[] {
  return tab === tabs[0] ? [...page] : [...page, tab];
}
