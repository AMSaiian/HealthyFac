/// <reference types="vite/client" />

/**
 * The router query parameters that the frontend may use: the modal (`compose`) and the view inputs that are not a
 * place (a range, a date, a metric, a staged selection, a dialog to open). A page and a tab are route segments.
 */
const ALLOWED_NAMES = new Set(['compose', 'draftId', 'draftAttemptId', 'from', 'to', 'metricKey', 'date']);
const NEWLINE = String.fromCharCode(10);
const STRING_CONSTANT = /const\s+([A-Z][A-Z0-9_]*)\s*=\s*'([^']*)'/g;
const QUERY_READ = /queryParamMap\.get\(\s*([^),\s]+)/g;
const QUERY_WRITE = /queryParams:\s*\{([^}]*)\}/g;
const WRITE_KEY = /(?:\[\s*([A-Za-z_][\w.]*)\s*\]|([A-Za-z_]\w*))\s*:/g;

/** Returns the parameter name of a key token: a string literal, a known string constant, or the bare property name. */
function nameOf(token: string, constants: ReadonlyMap<string, ReadonlySet<string>>, isProperty: boolean): string[] {
  const literal = /^'([^']*)'$/.exec(token);
  if (literal !== null) {
    return [literal[1]];
  }
  const values = constants.get(token);
  if (values !== undefined) {
    return [...values];
  }
  return isProperty ? [token] : [`<unresolved ${token}>`];
}

describe('route query params', { tags: ['Conventions'] }, () => {
  it('everyRouterQueryParam_IsAComposeOrAViewInput', () => {
    const sources = Object.entries(
      import.meta.glob(['./**/*.ts', '!./**/*.spec.ts'], { query: '?raw', import: 'default', eager: true }) as Record<
        string,
        string
      >,
    );
    const constants = new Map<string, Set<string>>();
    for (const [, source] of sources) {
      for (const [, name, value] of source.matchAll(STRING_CONSTANT)) {
        constants.set(name, (constants.get(name) ?? new Set()).add(value));
      }
    }

    const violations: string[] = [];
    function check(path: string, source: string, index: number, names: readonly string[]): void {
      for (const name of names.filter((candidate) => !ALLOWED_NAMES.has(candidate))) {
        violations.push(`${path}:${source.slice(0, index).split(NEWLINE).length} ${name}`);
      }
    }
    for (const [path, source] of sources) {
      for (const match of source.matchAll(QUERY_READ)) {
        check(path, source, match.index, nameOf(match[1], constants, false));
      }
      for (const match of source.matchAll(QUERY_WRITE)) {
        for (const [, computed, property] of match[1].matchAll(WRITE_KEY)) {
          const names = computed === undefined ? nameOf(property, constants, true) : nameOf(computed, constants, false);
          check(path, source, match.index, names);
        }
      }
    }

    expect(violations).toEqual([]);
  });
});
