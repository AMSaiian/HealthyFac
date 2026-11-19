const CSS_NODE_FS = 'node:fs';
const STYLESHEET_ROOT = 'src/app';
const STYLESHEET_EXTENSION = '.css';

const COMPONENT_TOKEN =
  /var\(--p-(datatable|select|datepicker|button|checkbox|tag|tabs|popover|menu|dialog|drawer|scroller|skeleton|inputtext|message|toast|toolbar|paginator|multiselect|tooltip|confirmdialog)-/g;

/** Lists the component-scoped PrimeNG variables of a stylesheet: PrimeNG writes them only after that component renders. */
function componentTokensIn(source: string): string[] {
  return [...source.matchAll(COMPONENT_TOKEN)].map((match) => match[0]);
}

/**
 * Reads every stylesheet under `src/app/` from the disk. An import of a `.css` file with `?raw` gives an empty text in
 * the Angular test builder, because the builder handles every stylesheet itself. The tests run in Node with the project
 * folder as the working directory.
 */
async function readStylesheets(): Promise<Record<string, string>> {
  const fs = (await import(/* @vite-ignore */ CSS_NODE_FS)) as {
    readdirSync(path: string, options: { recursive: boolean }): string[];
    readFileSync(path: string, encoding: string): string;
  };
  const paths = fs
    .readdirSync(STYLESHEET_ROOT, { recursive: true })
    .filter((name) => name.endsWith(STYLESHEET_EXTENSION))
    .map((name) => `${STYLESHEET_ROOT}/${name.replaceAll('\\', '/')}`);
  return Object.fromEntries(paths.map((path) => [path, fs.readFileSync(path, 'utf8')]));
}

describe('component css tokens', () => {
  it('componentCssTokens_EveryStylesheet_UsesOnlyGlobalTokens', async () => {
    const stylesheets = await readStylesheets();
    expect(Object.keys(stylesheets)).not.toHaveLength(0);
    expect(Object.values(stylesheets).every((source) => source.trim() !== '')).toBe(true);

    const offenders = Object.entries(stylesheets).flatMap(([path, source]) =>
      componentTokensIn(source).map((token) => `${path}: ${token}`),
    );

    expect(offenders).toEqual([]);
  });

  it('componentTokensIn_WithADatatableToken_ReportsIt', () => {
    const offenders = componentTokensIn('a { color: var(--p-datatable-header-cell-color); }');

    expect(offenders).toHaveLength(1);
  });

  it('componentTokensIn_WithAGlobalToken_ReportsNothing', () => {
    const offenders = componentTokensIn('a { color: var(--p-content-border-color); }');

    expect(offenders).toEqual([]);
  });
});
