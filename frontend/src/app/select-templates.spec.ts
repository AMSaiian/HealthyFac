/// <reference types="vite/client" />

/**
 * A select shows its values in the same form in the option list (`#item`) and in the selection (`#selectedItem`): a
 * value with a visual form is a tag in both, and a plain value is plain text in both. This spec reads every template and
 * fails for a `p-select` or `p-multiselect` whose two templates differ in form.
 */
const SELECT_OPEN = /<(p-select|p-multiselect)(?=[\s/>])/g;
const ITEM_TEMPLATE = /<ng-template\s+#item\b/;
const SELECTED_TEMPLATE = /<ng-template\s+#selectedItem\b/;
const TEMPLATE_OPEN = /<ng-template\b/g;
const TAG_ELEMENT = /<(p-tag|hf-file-type-tag)(?=[\s/>])/;
const CHIP_SUMMARY = /<hf-chip-summary\b[^>]*>/;
const TEXT_APPEARANCE = /\bappearance="text"/;
const WHITESPACE = /\s+/g;

/** The form that a select shows its values in. */
export enum SelectForm {
  Tag = 'tag',
  Text = 'text',
}

interface SelectBlock {
  readonly name: string;
  readonly source: string;
  readonly line: number;
}

/** Cuts the source of every `p-select` and `p-multiselect`: from its open tag to its closing tag, or to its `/>`. */
export function selectBlocks(source: string): SelectBlock[] {
  return [...source.matchAll(SELECT_OPEN)].map((match) => {
    const start = match.index;
    const name = match[1];
    const closing = `</${name}>`;
    const openTagEnd = source.indexOf('>', start);
    const isSelfClosing = source[openTagEnd - 1] === '/';
    const end = isSelfClosing ? openTagEnd + 1 : source.indexOf(closing, start) + closing.length;
    return {
      name,
      source: source.slice(start, end),
      line: source.slice(0, start).split('\n').length,
    };
  });
}

/** Cuts the content of the first `<ng-template #name>` of the block, up to its closing `</ng-template>`. */
function templateContent(block: string, opening: RegExp): string | null {
  const open = opening.exec(block);
  if (open === null) {
    return null;
  }
  let depth = 0;
  for (const match of block.slice(open.index).matchAll(new RegExp(`${TEMPLATE_OPEN.source}|</ng-template>`, 'g'))) {
    depth += match[0] === '</ng-template>' ? -1 : 1;
    if (depth === 0) {
      return block.slice(open.index, open.index + match.index + match[0].length);
    }
  }
  return block.slice(open.index);
}

/** The form of the option list: a tag when `#item` holds a tag, else plain text (also without a `#item`). */
export function itemForm(block: string): SelectForm {
  const item = templateContent(block, ITEM_TEMPLATE);
  return item !== null && TAG_ELEMENT.test(item) ? SelectForm.Tag : SelectForm.Text;
}

/**
 * The form of the selection: a tag when `#selectedItem` holds a tag or a chip summary with the tag appearance (its
 * default), else plain text (also without a `#selectedItem`).
 */
export function selectedForm(block: string): SelectForm {
  const selected = templateContent(block, SELECTED_TEMPLATE);
  if (selected === null) {
    return SelectForm.Text;
  }
  const summary = CHIP_SUMMARY.exec(selected);
  if (summary !== null) {
    return TEXT_APPEARANCE.test(summary[0]) ? SelectForm.Text : SelectForm.Tag;
  }
  return TAG_ELEMENT.test(selected) ? SelectForm.Tag : SelectForm.Text;
}

/** Lists the selects of a template whose option list and selection show their values in different forms. */
export function mixedSelects(source: string): string[] {
  return selectBlocks(source)
    .filter((block) => itemForm(block.source) !== selectedForm(block.source))
    .map(
      (block) =>
        `line ${block.line}: <${block.name}> shows ${itemForm(block.source)} in the list and ${selectedForm(block.source)} in the selection`,
    );
}

describe('select templates', { tags: ['Conventions'] }, () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('selectTemplates_EverySelect_ShowsItsValuesInTheSameFormInTheListAndInTheSelection', () => {
    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      mixedSelects(source).map((problem) => `${path} ${problem.replace(WHITESPACE, ' ')}`),
    );

    expect(offenders).toEqual([]);
  });

  it.each([
    [
      'TagsInTheListAndTextInTheSelection',
      '<p-select><ng-template #item let-o><p-tag [value]="o" /></ng-template><ng-template #selectedItem><hf-chip-summary appearance="text" /></ng-template></p-select>',
    ],
    [
      'PlainListAndTagsInTheSelection',
      '<p-select [options]="x"><ng-template #selectedItem><hf-chip-summary [chips]="c" /></ng-template></p-select>',
    ],
    [
      'TagsInTheListAndNoSelectionTemplate',
      '<p-multiselect><ng-template #item><p-tag value="x" /></ng-template></p-multiselect>',
    ],
  ])('mixedSelects_With_%s_ReportsTheSelect', (_name, html) => {
    expect(mixedSelects(html)).toHaveLength(1);
  });

  it.each([
    [
      'TagsInBoth',
      '<p-select><ng-template #item><p-tag value="x" /></ng-template><ng-template #selectedItem><hf-chip-summary /></ng-template></p-select>',
    ],
    [
      'TextInBoth',
      '<p-select><ng-template #selectedItem><hf-chip-summary appearance="text" /></ng-template></p-select>',
    ],
    ['NoTemplates', '<p-select [options]="x" />'],
    [
      'AFileTypeTagInBoth',
      '<p-select><ng-template #item><hf-file-type-tag /></ng-template><ng-template #selectedItem><hf-chip-summary /></ng-template></p-select>',
    ],
  ])('mixedSelects_With_%s_ReportsNothing', (_name, html) => {
    expect(mixedSelects(html)).toEqual([]);
  });
});
