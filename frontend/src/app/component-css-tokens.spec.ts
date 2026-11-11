/// <reference types="vite/client" />

const COMPONENT_TOKEN =
  /var\(--p-(datatable|select|datepicker|button|checkbox|tag|tabs|popover|menu|dialog|drawer|scroller|skeleton|inputtext|message|toast|toolbar|paginator|multiselect|tooltip|confirmdialog)-/g;

/** Lists the component-scoped PrimeNG variables of a stylesheet: PrimeNG writes them only after that component renders. */
function componentTokensIn(source: string): string[] {
  return [...source.matchAll(COMPONENT_TOKEN)].map((match) => match[0]);
}

describe('component css tokens', () => {
  const stylesheets = import.meta.glob('./**/*.css', {
    query: '?raw',
    import: 'default',
    eager: true,
  }) as Record<string, string>;

  it('componentCssTokens_EveryStylesheet_UsesOnlyGlobalTokens', () => {
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
