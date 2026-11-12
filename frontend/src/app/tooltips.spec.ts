/// <reference types="vite/client" />

const NEWLINE = String.fromCharCode(10);
const ELEMENT_START = /<(button|a|label|p-tab)\b/g;
const P_BUTTON = /\spButton[\s>=/]/;
const TOOLTIP_BINDING = /\s\[?pTooltip\]?=/;

/** Returns the end of the start tag that begins at `start`, skipping `>` inside quoted attribute values. */
function endOfStartTag(source: string, start: number): number {
  let quote: string | null = null;
  for (let index = start; index < source.length; index++) {
    const char = source[index];
    if (quote !== null) {
      quote = char === quote ? null : quote;
    } else if (char === '"' || char === "'") {
      quote = char;
    } else if (char === '>') {
      return index;
    }
  }
  return source.length;
}

/** The `file:line` of each `pButton` element and each `p-tab` without a `pTooltip`. */
function elementsWithoutTooltip(path: string, source: string): string[] {
  const found: string[] = [];
  for (const match of source.matchAll(ELEMENT_START)) {
    const start = match.index;
    const startTag = source.slice(start, endOfStartTag(source, start) + 1);
    const isChecked = match[1] === 'p-tab' || P_BUTTON.test(startTag);
    if (isChecked && !TOOLTIP_BINDING.test(startTag)) {
      found.push(`${path}:${source.slice(0, start).split(NEWLINE).length}`);
    }
  }
  return found;
}

describe('tooltips', () => {
  it('everyPButtonAndTab_BindsATooltip', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) => elementsWithoutTooltip(path, source));

    expect(Object.keys(templates).length).toBeGreaterThan(0);
    expect(offenders).toEqual([]);
  });

  it('elementsWithoutTooltip_WithAndWithoutTheBinding_NamesFileAndLine', () => {
    const source = [
      '<button pButton type="button">A</button>',
      '<button pButton [pTooltip]="a | translate">B</button>',
      '<a pButton routerLink="/x">C</a>',
      '<p-tab [value]="1">D</p-tab>',
      '<p-tab [pTooltip]="a" [value]="2">E</p-tab>',
      '<button type="button">F</button>',
    ].join(NEWLINE);

    const found = elementsWithoutTooltip('t.html', source);

    expect(found).toEqual(['t.html:1', 't.html:3', 't.html:4']);
  });
});
