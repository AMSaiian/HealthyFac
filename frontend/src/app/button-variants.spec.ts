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

const REQUIRED_BINDINGS: Record<string, readonly string[]> = {
  'p-column-filter': ['[filterButtonProps]="'],
  'p-dialog': ['[pt]="', '[dismissableMask]="'],
  'p-drawer': ['[dismissible]="', '[showCloseIcon]="false"'],
  'p-confirmdialog': ['[pt]="', '[dismissableMask]="'],
};
const COMPONENT_START = /<(p-column-filter|p-dialog|p-drawer|p-confirmdialog)(?=[\s>])/g;

/** The `file:line` of each `p-column-filter`, `p-dialog`, `p-drawer`, and `p-confirmdialog` without a required binding. */
function hostsWithoutRequiredBindings(path: string, source: string): string[] {
  const found: string[] = [];
  for (const match of source.matchAll(COMPONENT_START)) {
    const start = match.index;
    const startTag = source.slice(start, endOfStartTag(source, start) + 1);
    if (REQUIRED_BINDINGS[match[1]].some((binding) => !startTag.includes(binding))) {
      found.push(`${path}:${source.slice(0, start).split(NEWLINE).length}`);
    }
  }
  return found;
}

const CLOSABLE_FALSE = '[closable]="false"';
const HIDDEN_CLOSE_BUTTON_PT_NAME = 'HIDDEN_CLOSE_BUTTON_PT';
const DIALOG_START = /<p-(dialog|confirmdialog)(?=[\s>])/;

/** The `file:line` of each template line that sets `[closable]="false"`, which also turns off Escape and the backdrop. */
function closableFalseOffenders(path: string, source: string): string[] {
  return source
    .split(NEWLINE)
    .map((line, index) => (line.includes(CLOSABLE_FALSE) ? `${path}:${index + 1}` : null))
    .filter((found): found is string => found !== null);
}

/** Whether a template has a dialog or confirmation whose component does not bind the hidden close button pt. */
function dialogWithoutHiddenClosePt(template: string, component: string): boolean {
  return DIALOG_START.test(template) && !component.includes(HIDDEN_CLOSE_BUTTON_PT_NAME);
}

/** The `file:line` of each `pButton` start tag that `isOffender` accepts. */
function pButtonOffenders(path: string, source: string, isOffender: (startTag: string) => boolean): string[] {
  const found: string[] = [];
  for (const match of source.matchAll(ELEMENT_START)) {
    const start = match.index;
    const startTag = source.slice(start, endOfStartTag(source, start) + 1);
    if (/\spButton[\s>=]/.test(startTag) && isOffender(startTag)) {
      found.push(`${path}:${source.slice(0, start).split(NEWLINE).length}`);
    }
  }
  return found;
}

const isOutlinedNotDanger = (startTag: string): boolean =>
  startTag.includes('[outlined]="true"') && !startTag.includes('severity="danger"');
const isDangerNotOutlined = (startTag: string): boolean =>
  startTag.includes('severity="danger"') && !startTag.includes('[outlined]="true"');

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

  it('everyColumnFilterDialogAndDrawer_HasItsRequiredBindings', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) => hostsWithoutRequiredBindings(path, source));

    expect(offenders).toEqual([]);
  });

  it('hostsWithoutRequiredBindings_WithoutTheBindings_NamesEachFileAndLine', () => {
    const source = [
      '<p-column-filter [field]="a"></p-column-filter>',
      '<p-dialog [header]="h"></p-dialog>',
      '<p-drawer position="left"></p-drawer>',
    ].join(NEWLINE);

    const found = hostsWithoutRequiredBindings('h.html', source);

    expect(found).toEqual(['h.html:1', 'h.html:2', 'h.html:3']);
  });

  it('hostsWithoutRequiredBindings_WithAConfirmDialog_NeedsTheHiddenClosePtAndTheMask', () => {
    const source = ['<p-confirmdialog />', '<p-confirmdialog [pt]="hiddenClosePt" [dismissableMask]="true" />'].join(
      NEWLINE,
    );

    const found = hostsWithoutRequiredBindings('c.html', source);

    expect(found).toEqual(['c.html:1']);
  });

  it('closableFalseOffenders_WithClosableFalse_NamesFileAndLine', () => {
    const source = ['<p-dialog [closable]="false"></p-dialog>', '<p-dialog [pt]="hiddenClosePt"></p-dialog>'].join(
      NEWLINE,
    );

    const found = closableFalseOffenders('d.html', source);

    expect(found).toEqual(['d.html:1']);
  });

  it('dialogWithoutHiddenClosePt_WithoutTheConstantInTheComponent_IsReported', () => {
    expect(dialogWithoutHiddenClosePt('<p-dialog></p-dialog>', 'class A {}')).toBe(true);
    expect(dialogWithoutHiddenClosePt('<p-dialog></p-dialog>', 'pt = HIDDEN_CLOSE_BUTTON_PT;')).toBe(false);
    expect(dialogWithoutHiddenClosePt('<div></div>', 'class A {}')).toBe(false);
  });

  it('everyTemplate_NeverSetsClosableFalse', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) => closableFalseOffenders(path, source));

    expect(offenders).toEqual([]);
  });

  it('everyDialogAndConfirmation_BindsTheHiddenClosePt', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;
    const components = import.meta.glob(['./**/*.ts', '!./**/*.spec.ts'], {
      query: '?raw',
      import: 'default',
      eager: true,
    }) as Record<string, string>;

    const offenders = Object.entries(templates)
      .filter(([path, source]) => dialogWithoutHiddenClosePt(source, components[path.replace(/\.html$/, '.ts')] ?? ''))
      .map(([path]) => path);

    expect(offenders).toEqual([]);
  });

  it('hostsWithoutRequiredBindings_WithTheBindings_FindsNone', () => {
    const source = [
      '<p-column-filter [field]="a" [filterButtonProps]="squareFilterButtonProps"></p-column-filter>',
      '<p-dialog [header]="h" [pt]="hiddenClosePt" [dismissableMask]="true"></p-dialog>',
      '<p-drawer [dismissible]="true" [showCloseIcon]="false"></p-drawer>',
    ].join(NEWLINE);

    const found = hostsWithoutRequiredBindings('k.html', source);

    expect(found).toEqual([]);
  });

  it('everyOutlinedPButton_IsDanger', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      pButtonOffenders(path, source, isOutlinedNotDanger),
    );

    expect(offenders).toEqual([]);
  });

  it('everyDangerPButton_IsOutlined', () => {
    const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
      string,
      string
    >;

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      pButtonOffenders(path, source, isDangerNotOutlined),
    );

    expect(offenders).toEqual([]);
  });

  it('outlinedPButtons_WithOutlinedNonDanger_NamesFileAndLine', () => {
    const source = [
      '<button pButton [outlined]="true">A</button>',
      '<button pButton severity="danger" [outlined]="true">B</button>',
    ].join(NEWLINE);

    const found = pButtonOffenders('o.html', source, isOutlinedNotDanger);

    expect(found).toEqual(['o.html:1']);
  });

  it('dangerPButtons_WithFilledDanger_NamesFileAndLine', () => {
    const source = [
      '<button pButton severity="danger">A</button>',
      '<button pButton severity="danger" [outlined]="true">B</button>',
    ].join(NEWLINE);

    const found = pButtonOffenders('d.html', source, isDangerNotOutlined);

    expect(found).toEqual(['d.html:1']);
  });

  it.each(
    [
      {
        name: 'WithTextAndLinkVariants',
        file: 'x.html',
        source: [
          '<button pButton [text]="true">A</button>',
          '<button pButton variant="link">B</button>',
          '<button pButton text>C</button>',
          '<a pButton [link]="true">D</a>',
        ],
        expected: ['x.html:1', 'x.html:2', 'x.html:3', 'x.html:4'],
      },
      {
        name: 'WithOutlinedIconOnlyButtons',
        file: 'z.html',
        source: [
          '<button pButton iconOnly [outlined]="true"><i class="pi pi-bars"></i></button>',
          '<button pButton iconOnly outlined><i class="pi pi-bars"></i></button>',
        ],
        expected: ['z.html:1', 'z.html:2'],
      },
      {
        name: 'WithRoundedButtons',
        file: 'r.html',
        source: [
          '<button pButton iconOnly [text]="true" [rounded]="true"><i class="pi pi-bars"></i></button>',
          '<button pButton rounded>A</button>',
        ],
        expected: ['r.html:1', 'r.html:2'],
      },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('buttonsWithForbiddenVariant_%s_NamesEachFileAndLine', (_name, { file, source, expected }) => {
    expect(buttonsWithForbiddenVariant(file, source.join(NEWLINE))).toEqual(expected);
  });

  it.each(
    [
      {
        name: 'WithTextAndFilledIconOnlyButtons',
        file: 'w.html',
        source: [
          '<button pButton iconOnly [text]="true"><i class="pi pi-bars"></i></button>',
          '<button pButton iconOnly severity="secondary"><i class="pi pi-copy"></i></button>',
        ],
      },
      {
        name: 'WithFilledOutlinedAndPlainButtons',
        file: 'y.html',
        source: [
          '<button pButton>A</button>',
          '<button pButton [outlined]="true" [disabled]="rows.some((row) => row.isText)" (click)="go(a > b)">B</button>',
          '<button type="button" [text]="true">C</button>',
          '<button pButton pTooltip="a text tooltip" severity="secondary">D</button>',
        ],
      },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('buttonsWithForbiddenVariant_%s_FindsNone', (_name, { file, source }) => {
    expect(buttonsWithForbiddenVariant(file, source.join(NEWLINE))).toEqual([]);
  });
});
