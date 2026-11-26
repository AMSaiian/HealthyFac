/// <reference types="vite/client" />

/**
 * Every table lets the user resize its columns: each `p-table` sets `[resizableColumns]="true"` and
 * `columnResizeMode="expand"`, and each header cell with a label has `pResizableColumn`. This spec reads every
 * template and lists the tables and header cells that miss one.
 */
const TABLE_OPEN = /<p-table\b[^>]*>/g;
const HEADER_CELL = /<th\b([^>]*)>([\s\S]*?)<\/th>/g;
const RESIZABLE_BINDING = /\[resizableColumns\]="true"/;
const EXPAND_MODE = /\bcolumnResizeMode="expand"/;
const RESIZABLE_ATTRIBUTE = /\bpResizableColumn\b/;
const INTERPOLATION = '{{';

function lineOf(source: string, index: number): number {
  return source.slice(0, index).split('\n').length;
}

/** Lists the lines of the `p-table` elements without the resize bindings. */
export function tablesWithoutResize(source: string): number[] {
  return [...source.matchAll(TABLE_OPEN)]
    .filter((match) => !RESIZABLE_BINDING.test(match[0]) || !EXPAND_MODE.test(match[0]))
    .map((match) => lineOf(source, match.index));
}

/** Lists the lines of the header cells with a label that lack `pResizableColumn`. */
export function headerCellsWithoutResize(source: string): number[] {
  return [...source.matchAll(HEADER_CELL)]
    .filter((match) => match[2].includes(INTERPOLATION))
    .filter((match) => !RESIZABLE_ATTRIBUTE.test(match[1]))
    .map((match) => lineOf(source, match.index));
}

describe('grid resize', { tags: ['Conventions'] }, () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('gridResize_EveryTable_SetsTheResizeBindings', () => {
    expect(Object.keys(templates).length).toBeGreaterThan(0);

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      tablesWithoutResize(source).map((line) => `${path}:${line}`),
    );

    expect(offenders).toEqual([]);
  });

  it('gridResize_EveryLabelledHeaderCell_IsResizable', () => {
    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      headerCellsWithoutResize(source).map((line) => `${path}:${line}`),
    );

    expect(offenders).toEqual([]);
  });

  it('tablesWithoutResize_WithAPlainTable_ReportsIt', () => {
    expect(tablesWithoutResize('<div>\n<p-table [value]="rows">\n</p-table></div>')).toEqual([2]);
  });

  it('headerCellsWithoutResize_WithAResizableAndAnEmptyHeader_ReportsOnlyTheLabelledPlainOne', () => {
    const source = '<tr>\n<th pResizableColumn>{{ a }}</th>\n<th></th>\n<th class="w-4">{{ b }}</th>\n</tr>';

    expect(headerCellsWithoutResize(source)).toEqual([4]);
  });
});
