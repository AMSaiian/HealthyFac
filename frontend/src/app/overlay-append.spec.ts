/// <reference types="vite/client" />

const OVERLAY_TAG = /<(p-select|p-datepicker|p-menu|p-popover|p-multiselect)(?=[\s/>])[^>]*>/g;
const APPEND_TO = /\[?appendTo\]?=/;
const WHITESPACE = /\s+/g;

/** Lists the overlay tags of a template that do not set `appendTo`, so a scroll boundary could clip their panel. */
function overlaysWithoutAppendTo(source: string): string[] {
  return [...source.matchAll(OVERLAY_TAG)].map((match) => match[0]).filter((tag) => !APPEND_TO.test(tag));
}

describe('overlay append', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('overlayAppend_EveryOverlayTag_SetsAppendTo', () => {
    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      overlaysWithoutAppendTo(source).map((tag) => `${path}: ${tag.replace(WHITESPACE, ' ')}`),
    );

    expect(offenders).toEqual([]);
  });

  it('overlaysWithoutAppendTo_WithATagWithoutIt_ReportsTheTag', () => {
    const offenders = overlaysWithoutAppendTo('<p-select [options]="o" />');

    expect(offenders).toHaveLength(1);
  });

  it('overlaysWithoutAppendTo_WithAppendToOnAnotherLine_ReportsNothing', () => {
    const offenders = overlaysWithoutAppendTo('<p-datepicker [appendTo]="x" hourFormat="24" />');

    expect(offenders).toEqual([]);
  });
});
