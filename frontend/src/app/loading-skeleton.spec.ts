/// <reference types="vite/client" />

const SKELETON_TAG = '<p-skeleton';
const DELAYED_LOADING_CALL = 'delayedLoading(';
const HTML_SUFFIX = '.html';
const TS_SUFFIX = '.ts';

function componentWithoutDelay(templates: Record<string, string>, components: Record<string, string>): string[] {
  return Object.entries(templates)
    .filter(([, source]) => source.includes(SKELETON_TAG))
    .map(([path]) => path.slice(0, -HTML_SUFFIX.length) + TS_SUFFIX)
    .filter((path) => !(components[path] ?? '').includes(DELAYED_LOADING_CALL));
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

  it('loadingSkeleton_EveryComponentWithASkeleton_UsesDelayedLoading', () => {
    expect(componentWithoutDelay(templates, components)).toEqual([]);
  });

  it('componentWithoutDelay_WithASkeletonAndNoHelper_ReportsTheComponent', () => {
    const offenders = componentWithoutDelay({ './a/a.html': '<p-skeleton />' }, { './a/a.ts': 'class A {}' });

    expect(offenders).toEqual(['./a/a.ts']);
  });
});
