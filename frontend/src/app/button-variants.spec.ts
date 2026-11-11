/// <reference types="vite/client" />

const NEWLINE = String.fromCharCode(10);
const FORBIDDEN_VARIANTS = ['text', 'link'];
const ELEMENT_START = /<(button|a)\b/g;
const ATTRIBUTE_VALUE = /=\s*("[^"]*"|'[^']*')/g;
const VARIANT_VALUE = /\bvariant\s*=\s*["']([^"']*)["']/;

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

function hasForbiddenVariant(startTag: string): boolean {
  const names = startTag.replace(ATTRIBUTE_VALUE, '=');
  const variant = VARIANT_VALUE.exec(startTag)?.[1];
  return (
    FORBIDDEN_VARIANTS.some((name) => new RegExp(`[\\s\\[]${name}[\\]\\s=>]`).test(names)) ||
    (variant !== undefined && FORBIDDEN_VARIANTS.includes(variant))
  );
}

/** The `file:line` of each `pButton` element that has a text or link variant. */
function buttonsWithForbiddenVariant(path: string, source: string): string[] {
  const found: string[] = [];
  for (const match of source.matchAll(ELEMENT_START)) {
    const start = match.index;
    const startTag = source.slice(start, endOfStartTag(source, start) + 1);
    if (/\spButton[\s>=]/.test(startTag) && hasForbiddenVariant(startTag)) {
      found.push(`${path}:${source.slice(0, start).split('\n').length}`);
    }
  }
  return found;
}

describe('button variants', () => {
  it('everyPButton_IsFilledOrOutlined_NeverTextOrLink', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) => buttonsWithForbiddenVariant(path, source));

    expect(Object.keys(templates).length).toBeGreaterThan(0);
    expect(offenders).toEqual([]);
  });

  it('buttonsWithForbiddenVariant_WithTextAndLinkVariants_NamesEachFileAndLine', () => {
    const source = [
      '<button pButton [text]="true">A</button>',
      '<button pButton variant="link">B</button>',
      '<button pButton text>C</button>',
      '<a pButton [link]="true">D</a>',
    ].join(NEWLINE);

    const found = buttonsWithForbiddenVariant('x.html', source);

    expect(found).toEqual(['x.html:1', 'x.html:2', 'x.html:3', 'x.html:4']);
  });

  it('buttonsWithForbiddenVariant_WithFilledOutlinedAndPlainButtons_FindsNone', () => {
    const source = [
      '<button pButton>A</button>',
      '<button pButton [outlined]="true" [disabled]="rows.some((row) => row.isText)" (click)="go(a > b)">B</button>',
      '<button type="button" [text]="true">C</button>',
      '<button pButton pTooltip="a text tooltip" severity="secondary">D</button>',
    ].join(NEWLINE);

    const found = buttonsWithForbiddenVariant('y.html', source);

    expect(found).toEqual([]);
  });
});
