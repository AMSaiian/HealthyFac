/// <reference types="vite/client" />

const LOADING_MARKERS = ['<p-skeleton', '<hf-loading-spinner', '[loading]='];
const DELAYED_LOADING_CALLS = ['delayedLoading(', 'createGridTable'];
const ALREADY_DELAYED_COMPONENTS = ['./layout/shell/shell.ts'];
const HTML_SUFFIX = '.html';
const TS_SUFFIX = '.ts';

/**
 * The components whose template shows a skeleton, a spinner, or a `[loading]` state without `delayedLoading`. A grid
 * that calls `createGridTable` has it already (the helper delays `showSkeleton`). The shell is the one exception: its
 * navigation store delays the spinner already.
 */
function componentWithoutDelay(templates: Record<string, string>, components: Record<string, string>): string[] {
  return Object.entries(templates)
    .filter(([, source]) => LOADING_MARKERS.some((marker) => source.includes(marker)))
    .map(([path]) => path.slice(0, -HTML_SUFFIX.length) + TS_SUFFIX)
    .filter((path) => !ALREADY_DELAYED_COMPONENTS.includes(path))
    .filter((path) => !DELAYED_LOADING_CALLS.some((call) => (components[path] ?? '').includes(call)));
}

const LOADING_INPUT = 'isLoading = input(';

/** The components that declare an `isLoading` input, but whose template shows no loading state. */
function componentsWithLoadingInputButNoMarker(
  templates: Record<string, string>,
  components: Record<string, string>,
): string[] {
  return Object.entries(components)
    .filter(([, source]) => source.includes(LOADING_INPUT))
    .map(([path]) => path)
    .filter((path) => {
      const template = templates[path.slice(0, -TS_SUFFIX.length) + HTML_SUFFIX] ?? '';
      return !LOADING_MARKERS.some((marker) => template.includes(marker));
    });
}

const EMPTY_STATE_TAG = '<hf-empty-state';
const IF_OPEN = '@if (';
const ELSE_IF_OPEN = '@else if (';
const CONDITION_END = ') {';
const EMPTY_RESULT_GATES = ['showEmpty()', 'isEmptyResult'];
/** The empty states that show no empty result, with the reason. Each path names its whole template. */
const OTHER_EMPTY_STATES: Readonly<Record<string, string>> = {
  './files/ingest/summary/ingest-summary-host/ingest-summary-host.html':
    'the not-found state of a summary, not an empty result',
  './machines/metrics/metric-example-dialog/metric-example-dialog.html':
    'the state without an example text, which needs no load',
  './machines/metrics/metrics-page/metrics-page.html':
    'the "not registered" state and the "select a metric" prompt, not empty results',
};

/** The `@if` or `@else if` condition that opens each empty state of a template, or `''` when none opens it. */
function emptyStateConditions(source: string): string[] {
  const conditions: string[] = [];
  for (let index = source.indexOf(EMPTY_STATE_TAG); index !== -1; index = source.indexOf(EMPTY_STATE_TAG, index + 1)) {
    const before = source.slice(0, index);
    const opened = Math.max(before.lastIndexOf(IF_OPEN), before.lastIndexOf(ELSE_IF_OPEN));
    conditions.push(opened === -1 ? '' : before.slice(opened).split(CONDITION_END)[0]);
  }
  return conditions;
}

/** The templates with an empty state whose condition does not read the shared empty-result condition. */
function ungatedEmptyStates(templates: Record<string, string>): string[] {
  return Object.entries(templates)
    .filter(([path]) => !Object.keys(OTHER_EMPTY_STATES).includes(path))
    .filter(([, source]) =>
      emptyStateConditions(source).some((condition) => !EMPTY_RESULT_GATES.some((gate) => condition.includes(gate))),
    )
    .map(([path]) => path);
}

describe('loading skeleton', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;
  const components = import.meta.glob(['./**/*.ts', '!./**/*.spec.ts'], {
    query: '?raw',
    import: 'default',
    eager: true,
  }) as Record<string, string>;

  it('loadingSkeleton_EveryComponentWithALoadingState_UsesDelayedLoading', () => {
    expect(componentWithoutDelay(templates, components)).toEqual([]);
  });

  it.each([
    ['ASkeleton', '<p-skeleton />'],
    ['ASpinner', '<hf-loading-spinner />'],
    ['ALoadingBinding', '<p-select [loading]="isLoading()" />'],
  ])('componentWithoutDelay_With%sAndNoHelper_ReportsTheComponent', (_name, template) => {
    const offenders = componentWithoutDelay({ './a/a.html': template }, { './a/a.ts': 'class A {}' });

    expect(offenders).toEqual(['./a/a.ts']);
  });

  it('componentWithoutDelay_WithTheHelpersOrTheShell_ReportsNothing', () => {
    const offenders = componentWithoutDelay(
      {
        './a/a.html': '<hf-loading-spinner />',
        './b/b.html': '<p-skeleton />',
        './layout/shell/shell.html': '<hf-loading-spinner />',
      },
      { './a/a.ts': 'x = delayedLoading(y);', './b/b.ts': 'g = createGridTable({});', './layout/shell/shell.ts': '' },
    );

    expect(offenders).toEqual([]);
  });

  it('loadingSkeleton_EveryComponentWithALoadingInput_HasALoadingMarker', () => {
    expect(componentsWithLoadingInputButNoMarker(templates, components)).toEqual([]);
  });

  it('componentWithLoadingInput_WithoutALoadingMarker_IsReported', () => {
    const offenders = componentsWithLoadingInputButNoMarker(
      { './a/a.html': '<div></div>' },
      { './a/a.ts': 'readonly isLoading = input(false);' },
    );

    expect(offenders).toEqual(['./a/a.ts']);
  });

  it('componentWithLoadingInput_WithASkeleton_IsNotReported', () => {
    const offenders = componentsWithLoadingInputButNoMarker(
      { './a/a.html': '<p-skeleton />' },
      { './a/a.ts': 'readonly isLoading = input(false);' },
    );

    expect(offenders).toEqual([]);
  });

  it('everyEmptyState_IsGatedByTheSharedCondition', () => {
    expect(ungatedEmptyStates(templates)).toEqual([]);
  });

  it('ungatedEmptyStates_WithAnEmptyStateWithoutTheGate_ReportsTheTemplate', () => {
    const offenders = ungatedEmptyStates({
      './a/a.html': '@if (items().length === 0) {\n  <hf-empty-state />\n}',
    });

    expect(offenders).toEqual(['./a/a.html']);
  });

  it('ungatedEmptyStates_WithTheGate_ReportsNothing', () => {
    const offenders = ungatedEmptyStates({
      './a/a.html': '@if (grid.showEmpty()) {\n  <hf-empty-state />\n}',
    });

    expect(offenders).toEqual([]);
  });
});
