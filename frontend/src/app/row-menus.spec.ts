/// <reference types="vite/client" />

/**
 * A row menu renders through `hf-row-menu-popup`, so every grid menu has the same item look. Only the popup template and
 * the shell user menu hold a `<p-menu`.
 */
const MENU_OPEN = /<p-menu(?=[\s/>])/g;
const ALLOWED_TEMPLATES: readonly string[] = [
  './shared/grid/table/row-menu-popup/row-menu-popup.html',
  './layout/shell/shell.html',
];

function lineOf(source: string, index: number): number {
  return source.slice(0, index).split('\n').length;
}

/** Lists the lines of the `<p-menu` elements of a template. */
export function menuLines(source: string): number[] {
  return [...source.matchAll(MENU_OPEN)].map((match) => lineOf(source, match.index));
}

describe('row menus', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('rowMenus_EveryMenu_UsesTheSharedPopup', () => {
    expect(Object.keys(templates).length).toBeGreaterThan(0);

    const offenders = Object.entries(templates)
      .filter(([path]) => !ALLOWED_TEMPLATES.includes(path))
      .flatMap(([path, source]) => menuLines(source).map((line) => `${path}:${line}`));

    expect(offenders, `Use hf-row-menu-popup instead of p-menu in:\n${offenders.join('\n')}`).toEqual([]);
  });

  it('menuLines_WithMenusAndOtherElements_ListsOnlyTheMenuLines', () => {
    const source =
      '<div>\n<p-menu #menu [popup]="true" />\n<p-menubar></p-menubar>\n<p-menu\n  [model]="m">\n</p-menu>';

    expect(menuLines(source)).toEqual([2, 4]);
  });
});
