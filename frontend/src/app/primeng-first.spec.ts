/// <reference types="vite/client" />

const NATIVE_CONTROL = /<(select|textarea|input)\b[^>]*>/g;
const PRIMENG_DIRECTIVE = /\b(pInputText|pTextarea|pInputNumber|pCheckbox|pRadioButton)\b/;
const HIDDEN_OR_FILE = /type="(hidden|file)"/;
const SELECT_TAG = /^<select(?=[\s/>])/;
const PRIMENG_ONLY_INPUT_TYPE = /type="(checkbox|radio|date|time|datetime-local|number|range|color)"/;
const WHITESPACE = /\s+/g;
const EXCEPTIONS = ['./work-orders/board/', './shared/schedule/schedule-board/'];

/** Lists the native `select`, `textarea`, and `input` tags that carry no PrimeNG directive. */
function nativeControlsWithoutPrimeNg(source: string): string[] {
  return [...source.matchAll(NATIVE_CONTROL)]
    .map((match) => match[0])
    .filter(
      (tag) =>
        SELECT_TAG.test(tag) ||
        PRIMENG_ONLY_INPUT_TYPE.test(tag) ||
        (!PRIMENG_DIRECTIVE.test(tag) && !HIDDEN_OR_FILE.test(tag)),
    );
}

describe('PrimeNG first', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('primengFirst_EveryFormControl_UsesAPrimeNgDirective', () => {
    const offenders = Object.entries(templates)
      .filter(([path]) => !EXCEPTIONS.some((prefix) => path.startsWith(prefix)))
      .flatMap(([path, source]) =>
        nativeControlsWithoutPrimeNg(source).map((tag) => `${path}: ${tag.replace(WHITESPACE, ' ')}`),
      );

    expect(offenders).toEqual([]);
  });

  it('nativeControlsWithoutPrimeNg_WithABareInput_ReportsIt', () => {
    const offenders = nativeControlsWithoutPrimeNg('<input type="text" class="x" />');

    expect(offenders).toHaveLength(1);
  });

  it('nativeControlsWithoutPrimeNg_WithPInputText_ReportsNothing', () => {
    const offenders = nativeControlsWithoutPrimeNg('<input pInputText type="text" />');

    expect(offenders).toEqual([]);
  });

  it('nativeControlsWithoutPrimeNg_WithATimeInputThatHasPInputText_ReportsIt', () => {
    const offenders = nativeControlsWithoutPrimeNg('<input pInputText type="time" />');

    expect(offenders).toHaveLength(1);
  });

  it('nativeControlsWithoutPrimeNg_WithASelectThatHasPInputText_ReportsIt', () => {
    const offenders = nativeControlsWithoutPrimeNg('<select pInputText></select>');

    expect(offenders).toHaveLength(1);
  });
});
