/// <reference types="vite/client" />

const NEWLINE = String.fromCharCode(10);
const LABELLED_FORBIDDEN_VARIANTS = ['text', 'link', 'rounded'];
const ICON_ONLY_FORBIDDEN_VARIANTS = ['outlined', 'link', 'rounded'];
const ELEMENT_START = /<(button|a)\b/g;
const ATTRIBUTE_VALUE = /=\s*("[^"]*"|'[^']*')/g;
const ICON_ONLY = /\siconOnly[\s>=]/;
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
  const forbiddenVariants = ICON_ONLY.test(startTag) ? ICON_ONLY_FORBIDDEN_VARIANTS : LABELLED_FORBIDDEN_VARIANTS;
  return (
    forbiddenVariants.some((name) => new RegExp(`[\\s\\[]${name}[\\]\\s=>]`).test(names)) ||
    (variant !== undefined && forbiddenVariants.includes(variant))
  );
}

/**
 * The `file:line` of each `pButton` element with a forbidden variant: a labelled button with a text or link variant,
 * and an icon-only button (`iconOnly`) with an outlined or link variant, and any button with `rounded`.
 */
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

const REQUIRED_BINDINGS: Record<string, string> = {
  'p-column-filter': '[filterButtonProps]="',
  'p-dialog': '[closeButtonProps]="',
  'p-drawer': '[closeButtonProps]="',
  'p-confirmdialog': '[pt]="',
};
const COMPONENT_START = /<(p-column-filter|p-dialog|p-drawer|p-confirmdialog)(?=[\s>])/g;

/** The `file:line` of each `p-column-filter`, `p-dialog`, `p-drawer`, and `p-confirmdialog` without the square close button. */
function hostsWithoutSquareButtonProps(path: string, source: string): string[] {
  const found: string[] = [];
  for (const match of source.matchAll(COMPONENT_START)) {
    const start = match.index;
    const startTag = source.slice(start, endOfStartTag(source, start) + 1);
    if (!startTag.includes(REQUIRED_BINDINGS[match[1]])) {
      found.push(`${path}:${source.slice(0, start).split(NEWLINE).length}`);
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

  it('everyColumnFilterDialogAndDrawer_BindsTheSquareButtonProps', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      hostsWithoutSquareButtonProps(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('hostsWithoutSquareButtonProps_WithoutTheBindings_NamesEachFileAndLine', () => {
    const source = [
      '<p-column-filter [field]="a"></p-column-filter>',
      '<p-dialog [header]="h"></p-dialog>',
      '<p-drawer position="left"></p-drawer>',
    ].join(NEWLINE);

    const found = hostsWithoutSquareButtonProps('h.html', source);

    expect(found).toEqual(['h.html:1', 'h.html:2', 'h.html:3']);
  });

  it('hostsWithoutSquareButtonProps_WithAConfirmDialog_NeedsThePassThrough', () => {
    const source = ['<p-confirmdialog />', '<p-confirmdialog [pt]="squareConfirmDialogPt" />'].join(NEWLINE);

    const found = hostsWithoutSquareButtonProps('c.html', source);

    expect(found).toEqual(['c.html:1']);
  });

  it('hostsWithoutSquareButtonProps_WithTheBindings_FindsNone', () => {
    const source = [
      '<p-column-filter [field]="a" [filterButtonProps]="squareFilterButtonProps"></p-column-filter>',
      '<p-dialog [header]="h" [closeButtonProps]="squareCloseButtonProps"></p-dialog>',
      '<p-drawer [closeButtonProps]="squareCloseButtonProps"></p-drawer>',
    ].join(NEWLINE);

    const found = hostsWithoutSquareButtonProps('k.html', source);

    expect(found).toEqual([]);
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

  it('buttonsWithForbiddenVariant_WithOutlinedIconOnlyButtons_NamesEachFileAndLine', () => {
    const source = [
      '<button pButton iconOnly [outlined]="true"><i class="pi pi-bars"></i></button>',
      '<button pButton iconOnly outlined><i class="pi pi-bars"></i></button>',
    ].join(NEWLINE);

    const found = buttonsWithForbiddenVariant('z.html', source);

    expect(found).toEqual(['z.html:1', 'z.html:2']);
  });

  it('buttonsWithForbiddenVariant_WithRoundedButtons_NamesEachFileAndLine', () => {
    const source = [
      '<button pButton iconOnly [text]="true" [rounded]="true"><i class="pi pi-bars"></i></button>',
      '<button pButton rounded>A</button>',
    ].join(NEWLINE);

    const found = buttonsWithForbiddenVariant('r.html', source);

    expect(found).toEqual(['r.html:1', 'r.html:2']);
  });

  it('buttonsWithForbiddenVariant_WithTextAndFilledIconOnlyButtons_FindsNone', () => {
    const source = [
      '<button pButton iconOnly [text]="true"><i class="pi pi-bars"></i></button>',
      '<button pButton iconOnly severity="secondary"><i class="pi pi-copy"></i></button>',
    ].join(NEWLINE);

    const found = buttonsWithForbiddenVariant('w.html', source);

    expect(found).toEqual([]);
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
