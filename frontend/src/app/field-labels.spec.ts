/// <reference types="vite/client" />

/**
 * A field label (a `<label>` above an input, a select, a picker, an editor, or a group) and each label of a read-only
 * view carry `hf-field-label` (semibold). A choice label after its checkbox, radio, or toggle, and a `<label pButton>`,
 * keep the normal weight. This spec reads every template and fails for a label without the class.
 */
const LABEL_OPEN = /<label\b[^>]*>/g;
const FIELD_LABEL_CLASS = /\bclass="[^"]*\bhf-field-label\b/;
const BUTTON_ATTRIBUTE = /\bpButton\b/;
const CHOICE_ELEMENT_OPEN = /^<p-(checkbox|radiobutton|toggleswitch)(?=[\s/>])/;
const CHOICE_ELEMENT_CLOSE = /<\/p-(checkbox|radiobutton|toggleswitch)>$/;
const TRANSLATED_LABEL = /<label\b[^>]*>\s*\{\{\s*'([^']+)'\s*\|\s*translate\s*\}\}\s*<\/label>/g;
const TRANSLATED_ELEMENT = /<([\w-]+)\b([^>]*)>\s*\{\{\s*'([^']+)'\s*\|\s*translate\s*\}\}\s*<\/\1>/g;
const FIELD_LABEL_STYLESHEET = 'src/styles.css';
const FIELD_LABEL_NODE_FS = 'node:fs';
const FIELD_LABEL_RULE = /\.hf-field-label\s*\{[^}]*font-weight:\s*600\b/;

/** The read-only views of the five item dialogs and the forms that they mirror. */
const VIEW_FORM_PAIRS: readonly (readonly [string, string])[] = [
  ['./files/edit/file-details/file-details.html', './files/edit/file-edit-form/file-edit-form.html'],
  ['./machines/drawer/machine-details/machine-details.html', './machines/drawer/machine-form/machine-form.html'],
  ['./machines/phases/phase-details/phase-details.html', './machines/phases/phase-form/phase-form.html'],
  [
    './machines/statuses/drawer/status-definition-details/status-definition-details.html',
    './machines/statuses/drawer/status-definition-form/status-definition-form.html',
  ],
  [
    './work-orders/drawer/work-order-details/work-order-details.html',
    './work-orders/drawer/work-order-form/work-order-form.html',
  ],
];

function lineOf(source: string, index: number): number {
  return source.slice(0, index).split('\n').length;
}

/** Tells whether the text before a label ends with a checkbox, a radio button, or a toggle element. */
function followsChoiceControl(before: string): boolean {
  const trimmed = before.trimEnd();
  if (CHOICE_ELEMENT_CLOSE.test(trimmed)) {
    return true;
  }
  return trimmed.endsWith('/>') && CHOICE_ELEMENT_OPEN.test(trimmed.slice(trimmed.lastIndexOf('<')));
}

/** Lists the lines of the field labels that lack `hf-field-label`: not a `pButton` label and not a choice label. */
export function labelsWithoutClass(source: string): number[] {
  return [...source.matchAll(LABEL_OPEN)]
    .filter((match) => !FIELD_LABEL_CLASS.test(match[0]))
    .filter((match) => !BUTTON_ATTRIBUTE.test(match[0]))
    .filter((match) => !followsChoiceControl(source.slice(0, match.index)))
    .map((match) => lineOf(source, match.index));
}

/** Collects the translation keys of the form labels. */
export function labelKeys(formSource: string): Set<string> {
  return new Set([...formSource.matchAll(TRANSLATED_LABEL)].map((match) => match[1]));
}

/** Lists the lines of the view elements that show only a form label key and lack `hf-field-label`. */
export function viewLabelsWithoutClass(viewSource: string, keys: ReadonlySet<string>): number[] {
  return [...viewSource.matchAll(TRANSLATED_ELEMENT)]
    .filter((match) => keys.has(match[3]))
    .filter((match) => !FIELD_LABEL_CLASS.test(`<${match[1]}${match[2]}>`))
    .map((match) => lineOf(viewSource, match.index));
}

/**
 * Reads the stylesheet from the disk. An import of a `.css` file with `?raw` gives an empty text in the Angular test
 * builder. The tests run in Node with the project folder as the working directory.
 */
async function readStylesheet(): Promise<string> {
  const fs = (await import(/* @vite-ignore */ FIELD_LABEL_NODE_FS)) as {
    readFileSync(path: string, encoding: string): string;
  };
  return fs.readFileSync(FIELD_LABEL_STYLESHEET, 'utf8');
}

describe('field labels', () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('fieldLabels_EveryLabel_CarriesTheFieldLabelClass', () => {
    expect(Object.keys(templates).length).toBeGreaterThan(0);

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      labelsWithoutClass(source).map((line) => `${path}:${line}`),
    );

    expect(offenders).toEqual([]);
  });

  it('fieldLabels_EveryViewLabel_CarriesTheFieldLabelClass', () => {
    const offenders = VIEW_FORM_PAIRS.flatMap(([viewPath, formPath]) => {
      expect(templates[viewPath], viewPath).toBeDefined();
      expect(templates[formPath], formPath).toBeDefined();
      return viewLabelsWithoutClass(templates[viewPath], labelKeys(templates[formPath])).map(
        (line) => `${viewPath}:${line}`,
      );
    });

    expect(offenders).toEqual([]);
  });

  it('fieldLabels_WorkOrderFilesLabel_CarriesTheFieldLabelClass', () => {
    const source = templates['./work-orders/drawer/work-order-files/work-order-files.html'];

    expect(source).toBeDefined();
    expect(viewLabelsWithoutClass(source, new Set(['WORK_ORDERS.FORM.FILES']))).toEqual([]);
  });

  it('fieldLabels_BlockListEditorLabel_CarriesTheFieldLabelClass', () => {
    const source = templates['./employees/shifts/block-list-editor/block-list-editor.html'];

    expect(source).toBeDefined();

    const label = /<(\w+)\b([^>]*)>\s*\{\{\s*label\(\)\s*\}\}\s*<\/\1>/.exec(source);

    expect(label).not.toBeNull();
    expect(FIELD_LABEL_CLASS.test(`<${label![1]}${label![2]}>`)).toBe(true);
  });

  it('fieldLabels_Stylesheet_SetsTheWeight', async () => {
    const stylesheet = await readStylesheet();

    expect(stylesheet).toMatch(FIELD_LABEL_RULE);
  });

  it('labelsWithoutClass_WithAPlainLabel_ReportsIt', () => {
    expect(labelsWithoutClass('<div>\n<label for="x">A</label>\n</div>')).toEqual([2]);
  });

  it('labelsWithoutClass_WithAChoiceLabelAndAButtonLabel_ReportsNothing', () => {
    const source =
      '<p-checkbox inputId="x" [formField]="f" /><label for="x">A</label>\n<label pButton>B</label>\n' +
      '<p-toggleswitch></p-toggleswitch>\n<label for="y">C</label>';

    expect(labelsWithoutClass(source)).toEqual([]);
  });
});
