/// <reference types="vite/client" />

const FIXED_SHADE = /(?<![:\w-])(bg|text|border)-surface-\d+/g;
const FIXED_COLOR = /(?<![:\w-])(bg-white|bg-black|text-black)(?![\w-])/;
const CLASS_ATTRIBUTE = /\bclass="([^"]*)"/g;
const HOST_CLASS = /\bclass:\s*'([^']*)'/g;

function classLists(source: string): string[] {
  return [...source.matchAll(CLASS_ATTRIBUTE), ...source.matchAll(HOST_CLASS)].map((match) => match[1]);
}

/** Lists each class list with a fixed color of one theme: a surface shade without the `dark:` pair of its prefix. */
function lightOnlyLists(source: string): string[] {
  return classLists(source).filter((list) => {
    const hasFixedColor = FIXED_COLOR.test(list);
    const hasUnpairedShade = [...list.matchAll(FIXED_SHADE)].some(
      (match) => !list.includes(`dark:${match[1]}-surface-`),
    );
    return hasFixedColor || hasUnpairedShade;
  });
}

describe('theme classes', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true });
  const components = import.meta.glob(['./**/*.ts', '!./**/*.spec.ts'], {
    query: '?raw',
    import: 'default',
    eager: true,
  });

  it('themeClasses_EveryTemplateAndHostClass_FollowsBothThemes', () => {
    const sources = { ...templates, ...components } as Record<string, string>;
    const offenders = Object.entries(sources).flatMap(([path, source]) =>
      lightOnlyLists(source).map((list) => `${path}: ${list.replace(/\s+/g, ' ').trim()}`),
    );

    expect(offenders).toEqual([]);
  });

  it.each(
    [
      { name: 'WithAnUnpairedShade', html: '<div class="bg-surface-50 flex"></div>' },
      { name: 'WithOnlyTheOtherPrefixPaired', html: '<div class="bg-surface-50 dark:text-surface-100"></div>' },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('lightOnlyLists_%s_ReportsIt', (_name, { html }) => {
    expect(lightOnlyLists(html)).toHaveLength(1);
  });

  it('lightOnlyLists_WithTheDarkPair_ReportsNothing', () => {
    const offenders = lightOnlyLists('<div class="bg-surface-50 dark:bg-surface-900 border-surface"></div>');

    expect(offenders).toEqual([]);
  });
});
