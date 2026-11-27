/// <reference types="vite/client" />

/**
 * Every grid has a refresh button. A grid component (it calls `createGridTable`) shows `hf-grid-refresh-button`, a grid
 * store (it calls `withGridState`) composes `withGridRefresh`, and each host template of a grid component binds
 * `[refreshSecondsLeft]` and `(refreshRequested)`. This spec reads the sources and lists the offenders.
 */
const SHARED_GRID_FOLDER = './shared/grid/';
const GRID_TABLE_CALL = /\bcreateGridTable\s*[<(]/;
const GRID_STATE_CALL = /\bwithGridState\s*[<(]/;
const GRID_REFRESH_CALL = /\bwithGridRefresh\s*[<(]/;
const REFRESH_BUTTON = /<hf-grid-refresh-button\b/;
const SELECTOR = /\bselector:\s*'([^']+)'/;
const TEMPLATE_URL = /\btemplateUrl:\s*'\.\/([^']+)'/;
const REFRESH_OUTPUT_BINDING = /\(refreshRequested\)\s*=/;
const REFRESH_SECONDS_BINDING = /\[refreshSecondsLeft\]\s*=/;

function lineOf(source: string, index: number): number {
  return source.slice(0, index).split('\n').length;
}

function folderOf(path: string): string {
  return path.slice(0, path.lastIndexOf('/') + 1);
}

/** The path of the template that a grid component file names, next to the file; `null` when it names none. */
export function templatePathOf(componentPath: string, source: string): string | null {
  const templateUrl = TEMPLATE_URL.exec(source)?.[1];
  return templateUrl === undefined ? null : `${folderOf(componentPath)}${templateUrl}`;
}

/** The selector of a grid component, or `null` when the source is no grid component. */
export function gridSelectorOf(source: string): string | null {
  return GRID_TABLE_CALL.test(source) ? (SELECTOR.exec(source)?.[1] ?? null) : null;
}

/** Whether a grid template holds the refresh button. */
export function hasRefreshButton(template: string): boolean {
  return REFRESH_BUTTON.test(template);
}

/** Lists the lines of the elements `<selector ...>` that miss `(refreshRequested)` or `[refreshSecondsLeft]`. */
export function hostsWithoutRefresh(template: string, selector: string): number[] {
  const start = new RegExp(`<${selector}\\b([^>]*)>`, 'g');
  return [...template.matchAll(start)]
    .filter((match) => !REFRESH_OUTPUT_BINDING.test(match[1]) || !REFRESH_SECONDS_BINDING.test(match[1]))
    .map((match) => lineOf(template, match.index));
}

/** Whether a grid store source composes `withGridRefresh` (a source without `withGridState` needs none). */
export function isStoreWithoutRefresh(source: string): boolean {
  return GRID_STATE_CALL.test(source) && !GRID_REFRESH_CALL.test(source);
}

describe('grid refresh', { tags: ['Conventions'] }, () => {
  const sources = import.meta.glob('./**/*.ts', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;
  const productionSources = Object.entries(sources).filter(
    ([path]) => !path.endsWith('.spec.ts') && !path.startsWith(SHARED_GRID_FOLDER),
  );
  const gridComponents = productionSources.filter(([, source]) => gridSelectorOf(source) !== null);

  it('gridRefresh_EveryGridComponent_ShowsTheRefreshButton', () => {
    expect(gridComponents.length).toBeGreaterThan(0);

    const offenders = gridComponents
      .filter(([path, source]) => {
        const templatePath = templatePathOf(path, source);
        return templatePath === null || !hasRefreshButton(templates[templatePath] ?? '');
      })
      .map(([path]) => path);

    expect(offenders).toEqual([]);
  });

  it('gridRefresh_EveryGridStore_ComposesWithGridRefresh', () => {
    const stores = productionSources.filter(([path]) => path.endsWith('.store.ts'));
    expect(stores.length).toBeGreaterThan(0);

    const offenders = stores.filter(([, source]) => isStoreWithoutRefresh(source)).map(([path]) => path);

    expect(offenders).toEqual([]);
  });

  it('gridRefresh_EveryGridHost_WiresTheRefresh', () => {
    const selectors = gridComponents.map(([, source]) => gridSelectorOf(source) as string);

    const offenders = Object.entries(templates).flatMap(([path, template]) =>
      selectors.flatMap((selector) => hostsWithoutRefresh(template, selector).map((line) => `${path}:${line}`)),
    );

    expect(offenders).toEqual([]);
  });

  it('hasRefreshButton_WithAGridTemplateWithoutTheButton_ReportsIt', () => {
    expect(hasRefreshButton('<p-table [value]="rows"></p-table>')).toBe(false);
    expect(hasRefreshButton('<th><hf-grid-refresh-button /></th>')).toBe(true);
  });

  it('hostsWithoutRefresh_WithAHostWithoutTheOutput_ReportsItAtItsLine', () => {
    const template = '<div>\n<hf-machine-grid [refreshSecondsLeft]="store.refreshSecondsLeft()" />\n</div>';

    expect(hostsWithoutRefresh(template, 'hf-machine-grid')).toEqual([2]);
  });

  it('hostsWithoutRefresh_WithAHostWithBothBindings_ReportsNothing', () => {
    const template =
      '<hf-machine-grid\n  [refreshSecondsLeft]="store.refreshSecondsLeft()"\n  (refreshRequested)="store.refresh()"\n/>';

    expect(hostsWithoutRefresh(template, 'hf-machine-grid')).toEqual([]);
  });
});
