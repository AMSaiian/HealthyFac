/// <reference types="vite/client" />

const NEWLINE = String.fromCharCode(10);
const BOARD_PAGE_TEMPLATE = 'board-page.html';
const TABLE_START = /<p-table(?=[\s>])/g;
const COLUMN_FILTER_START = /<p-column-filter(?=[\s>])/g;
const COLUMN_FILTER_END = '</p-column-filter>';
const SELECT_START = /<p-select(?=[\s>])/g;
const POPOVER_START = /<p-popover(?=[\s>])/g;
const POPOVER_END = '</p-popover>';
const MULTIPLE_BINDING = '[multiple]="true"';
const OPTION_LIST_BINDING = 'panelStyleClass="hf-option-list"';
const OPTION_LIST_CLASS = 'hf-option-list';
const LISTBOX_START = /<p-listbox(?=[\s>])/g;
const CHECK_LIST_CLASS = 'hf-check-list';
const FILTER_TEMPLATE_START = /<ng-template\s+#filter\b/g;
const NG_TEMPLATE_TOKEN = /<ng-template\b|<\/ng-template>/g;
const NG_TEMPLATE_CLOSE = '</ng-template>';
const FILTER_CONTROL_START = /<(input|p-select|p-datepicker|hf-plain-date-range-picker)(?=[\s>])/g;
const PLACEHOLDER_BINDING = /\[?placeholder\]?=/;

interface Range {
  readonly start: number;
  readonly end: number;
}

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

function lineOf(path: string, source: string, index: number): string {
  return `${path}:${source.slice(0, index).split(NEWLINE).length}`;
}

/** The ranges from each opening tag to its closing tag, so a check can ask what an element holds. */
function elementRanges(source: string, startPattern: RegExp, endTag: string): Range[] {
  return [...source.matchAll(startPattern)].map((match) => {
    const closeIndex = source.indexOf(endTag, match.index);
    return { start: match.index, end: closeIndex === -1 ? source.length : closeIndex + endTag.length };
  });
}

/** The `file:line` of each `p-table` that does not set `[stripedRows]="true"`. */
function tablesWithoutStripedRows(path: string, source: string): string[] {
  return [...source.matchAll(TABLE_START)]
    .filter((match) => !source.slice(match.index, endOfStartTag(source, match.index)).includes('[stripedRows]="true"'))
    .map((match) => lineOf(path, source, match.index));
}

/** The `file:line` of each `p-column-filter` that shows the apply button or has no `(onHide)` handler. */
function columnFiltersWithoutCloseApply(path: string, source: string): string[] {
  return [...source.matchAll(COLUMN_FILTER_START)]
    .filter((match) => {
      const startTag = source.slice(match.index, endOfStartTag(source, match.index));
      return !startTag.includes('[showApplyButton]="false"') || !startTag.includes('(onHide)=');
    })
    .map((match) => lineOf(path, source, match.index));
}

/**
 * The `file:line` of each multiple `p-select` without the option list class, inside a `p-column-filter`
 * and anywhere in the board page template.
 */
function multiSelectsWithoutOptionList(path: string, source: string): string[] {
  const filters = elementRanges(source, COLUMN_FILTER_START, COLUMN_FILTER_END);
  const isBoardPage = path.endsWith(BOARD_PAGE_TEMPLATE);
  return [...source.matchAll(SELECT_START)]
    .filter((match) => {
      const startTag = source.slice(match.index, endOfStartTag(source, match.index));
      const isChecked = isBoardPage || filters.some((range) => match.index > range.start && match.index < range.end);
      return isChecked && startTag.includes(MULTIPLE_BINDING) && !startTag.includes(OPTION_LIST_BINDING);
    })
    .map((match) => lineOf(path, source, match.index));
}

/** The `file:line` of each `p-popover` that holds a `p-checkbox` and no `hf-check-list` class. */
function checkboxPopoversWithoutCheckList(path: string, source: string): string[] {
  return elementRanges(source, POPOVER_START, POPOVER_END)
    .filter((range) => {
      const body = source.slice(range.start, range.end);
      return body.includes('<p-checkbox') && !body.includes(CHECK_LIST_CLASS);
    })
    .map((range) => lineOf(path, source, range.start));
}

/** The ranges of the `#filter` templates. A nested `ng-template` does not end the range of its filter. */
function filterRanges(source: string): Range[] {
  const tokens = [...source.matchAll(NG_TEMPLATE_TOKEN)];
  return [...source.matchAll(FILTER_TEMPLATE_START)].map((start) => {
    let depth = 0;
    for (const token of tokens.filter((candidate) => candidate.index >= start.index)) {
      depth += token[0] === NG_TEMPLATE_CLOSE ? -1 : 1;
      if (depth === 0) {
        return { start: start.index, end: token.index + token[0].length };
      }
    }
    return { start: start.index, end: source.length };
  });
}

/** The `file:line` of each filter control (input, select, or date picker) without a placeholder. */
function filterControlsWithoutPlaceholder(path: string, source: string): string[] {
  const filters = filterRanges(source);
  return [...source.matchAll(FILTER_CONTROL_START)]
    .filter((match) => filters.some((range) => match.index > range.start && match.index < range.end))
    .filter((match) => !PLACEHOLDER_BINDING.test(source.slice(match.index, endOfStartTag(source, match.index))))
    .map((match) => lineOf(path, source, match.index));
}

interface CaseBlock extends Range {
  readonly column: string;
}

const CASE_START = /@case \(([^)]+)\) \{/g;
const CHIP_SUMMARY_TAG = '<hf-chip-summary';
const CHIP_SUMMARY_START = /<hf-chip-summary(?=[\s>])/g;
const TEXT_APPEARANCE_BINDING = 'appearance="text"';
const P_TAG_ELEMENT = '<p-tag';
const SELECT_END = '</p-select>';
const SELECTED_ITEM = '#selectedItem';
const FOR_BLOCK = '@for';

/** Returns the end of the `{ }` block whose opening brace is at `openIndex`, after its closing brace. */
function braceBlockEnd(source: string, openIndex: number): number {
  let depth = 0;
  for (let index = openIndex; index < source.length; index++) {
    if (source[index] === '{') {
      depth++;
    } else if (source[index] === '}') {
      depth--;
      if (depth === 0) {
        return index + 1;
      }
    }
  }
  return source.length;
}

/** The `@case (column) { }` blocks of a template. */
function caseBlocks(source: string): CaseBlock[] {
  return [...source.matchAll(CASE_START)].map((match) => {
    const openIndex = match.index + match[0].length - 1;
    return { column: match[1], start: match.index, end: braceBlockEnd(source, openIndex) };
  });
}

/** Tells whether a filter case is a tag filter: a `p-tag` item and a chip summary that is not a text chip summary. */
function isTagFilterCase(filterCase: string): boolean {
  const chipSummaryTags = [...filterCase.matchAll(CHIP_SUMMARY_START)].map((match) =>
    filterCase.slice(match.index, endOfStartTag(filterCase, match.index)),
  );
  return filterCase.includes(P_TAG_ELEMENT) && chipSummaryTags.some((tag) => !tag.includes(TEXT_APPEARANCE_BINDING));
}

/** The `file:line` of each body case with a `p-tag` whose filter case of the same column is not a tag filter. */
function tagColumnsWithoutTagFilter(path: string, source: string): string[] {
  const filters = filterRanges(source);
  const cases = caseBlocks(source);
  const isInFilter = (block: Range): boolean =>
    filters.some((range) => block.start > range.start && block.end < range.end);
  return cases
    .filter((bodyCase) => !isInFilter(bodyCase) && source.slice(bodyCase.start, bodyCase.end).includes(P_TAG_ELEMENT))
    .filter((bodyCase) => {
      const filterCase = cases.find((block) => block.column === bodyCase.column && isInFilter(block));
      return filterCase === undefined || !isTagFilterCase(source.slice(filterCase.start, filterCase.end));
    })
    .map((bodyCase) => lineOf(path, source, bodyCase.start));
}

/** The `#selectedItem` template of the select that starts at `start`, or an empty string when it has none. */
function selectedItemOf(source: string, start: number): string {
  const selectEnd = source.indexOf(SELECT_END, start);
  const element = source.slice(start, selectEnd === -1 ? source.length : selectEnd);
  const itemStart = element.indexOf(SELECTED_ITEM);
  if (itemStart === -1) {
    return '';
  }
  const itemEnd = element.indexOf(NG_TEMPLATE_CLOSE, itemStart);
  return element.slice(itemStart, itemEnd === -1 ? element.length : itemEnd);
}

/** The `file:line` of each multiple `p-select` in a filter or on the board without a chip summary selected item. */
function multiSelectsWithoutChipSummary(path: string, source: string): string[] {
  const filters = filterRanges(source);
  const isBoardPage = path.endsWith(BOARD_PAGE_TEMPLATE);
  return [...source.matchAll(SELECT_START)]
    .filter((match) => {
      const startTag = source.slice(match.index, endOfStartTag(source, match.index));
      const isChecked = isBoardPage || filters.some((range) => match.index > range.start && match.index < range.end);
      const selectedItem = selectedItemOf(source, match.index);
      const isChipSummary = selectedItem.includes(CHIP_SUMMARY_TAG) && !selectedItem.includes(FOR_BLOCK);
      return isChecked && startTag.includes(MULTIPLE_BINDING) && !isChipSummary;
    })
    .map((match) => lineOf(path, source, match.index));
}

/** The `file:line` of each `p-listbox` whose start tag does not set the `hf-option-list` class. */
function listboxesWithoutOptionList(path: string, source: string): string[] {
  return [...source.matchAll(LISTBOX_START)]
    .filter((match) => !source.slice(match.index, endOfStartTag(source, match.index)).includes(OPTION_LIST_CLASS))
    .map((match) => lineOf(path, source, match.index));
}

function allTemplates(): Record<string, string> {
  return import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<string, string>;
}

describe('grid and filter conventions', () => {
  it('everyTable_SetsStripedRows', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) => tablesWithoutStripedRows(path, source));

    expect(Object.keys(templates).length).toBeGreaterThan(0);
    expect(offenders).toEqual([]);
  });

  it('everyColumnFilter_AppliesOnClose', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      columnFiltersWithoutCloseApply(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('everyMultiSelectOfAFilter_UsesTheOptionListClass', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      multiSelectsWithoutOptionList(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('everyPopoverWithCheckboxes_UsesTheCheckListClass', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      checkboxPopoversWithoutCheckList(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('tablesWithoutStripedRows_WithAndWithoutTheBinding_NamesFileAndLine', () => {
    const source = ['<p-table [value]="a"></p-table>', '<p-table [value]="b" [stripedRows]="true"></p-table>'].join(
      NEWLINE,
    );

    const found = tablesWithoutStripedRows('t.html', source);

    expect(found).toEqual(['t.html:1']);
  });

  it('columnFiltersWithoutCloseApply_WithApplyButtonOrNoHandler_NamesFileAndLine', () => {
    const source = [
      '<p-column-filter [field]="a" [showApplyButton]="false" (onHide)="h()"></p-column-filter>',
      '<p-column-filter [field]="b" (onHide)="h()"></p-column-filter>',
      '<p-column-filter [field]="c" [showApplyButton]="false"></p-column-filter>',
    ].join(NEWLINE);

    const found = columnFiltersWithoutCloseApply('c.html', source);

    expect(found).toEqual(['c.html:2', 'c.html:3']);
  });

  it('multiSelectsWithoutOptionList_InsideAColumnFilter_NamesFileAndLine', () => {
    const source = [
      '<p-column-filter [field]="a">',
      '<p-select [multiple]="true" panelStyleClass="hf-option-list"></p-select>',
      '<p-select [multiple]="true"></p-select>',
      '</p-column-filter>',
      '<p-select [multiple]="true"></p-select>',
    ].join(NEWLINE);

    const found = multiSelectsWithoutOptionList('f.html', source);

    expect(found).toEqual(['f.html:3']);
  });

  it('multiSelectsWithoutOptionList_InTheBoardPage_NamesFileAndLine', () => {
    const source = ['<p-select [multiple]="true"></p-select>', '<p-select></p-select>'].join(NEWLINE);

    const found = multiSelectsWithoutOptionList('work-orders/board/board-page/board-page.html', source);

    expect(found).toEqual(['work-orders/board/board-page/board-page.html:1']);
  });

  it('checkboxPopoversWithoutCheckList_WithAndWithoutTheClass_NamesFileAndLine', () => {
    const source = [
      '<p-popover><div class="hf-check-list"><p-checkbox /></div></p-popover>',
      '<p-popover><div><p-checkbox /></div></p-popover>',
      '<p-popover><div>text</div></p-popover>',
    ].join(NEWLINE);

    const found = checkboxPopoversWithoutCheckList('p.html', source);

    expect(found).toEqual(['p.html:2']);
  });

  it('everyTagColumn_HasATagFilter', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) => tagColumnsWithoutTagFilter(path, source));

    expect(offenders).toEqual([]);
  });

  it('everyMultiSelectFilter_UsesTheChipSummary', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      multiSelectsWithoutChipSummary(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('tagColumnsWithoutTagFilter_WithATextFilterOnATagColumn_NamesFileAndLine', () => {
    const source = [
      '@case (Column.Status) {',
      '<p-tag [value]="file.status" />',
      '}',
      '<ng-template #filter let-value>',
      '@case (Column.Status) {',
      '<p-select [multiple]="true"><ng-template #item><p-tag /></ng-template><ng-template #selectedItem>' +
        '<hf-chip-summary appearance="text" [chips]="chips" /></ng-template></p-select>',
      '}',
      '</ng-template>',
    ].join(NEWLINE);

    const found = tagColumnsWithoutTagFilter('t.html', source);

    expect(found).toEqual(['t.html:1']);
  });

  it('tagColumnsWithoutTagFilter_WithATagFilterOnATagColumn_ReturnsNothing', () => {
    const source = [
      '@case (Column.Status) {',
      '<p-tag [value]="file.status" />',
      '}',
      '<ng-template #filter let-value>',
      '@case (Column.Status) {',
      '<p-select [multiple]="true"><ng-template #item><p-tag /></ng-template><ng-template #selectedItem>' +
        '<hf-chip-summary [chips]="chips" /></ng-template></p-select>',
      '}',
      '</ng-template>',
    ].join(NEWLINE);

    const found = tagColumnsWithoutTagFilter('t.html', source);

    expect(found).toEqual([]);
  });

  it('multiSelectsWithoutChipSummary_WithAForOrNoSelectedItem_NamesFileAndLine', () => {
    const source = [
      '<ng-template #filter let-value>',
      '<p-select [multiple]="true"><ng-template #selectedItem>@for (item of value; track item) {' +
        '<span>{{ item }}</span>}</ng-template></p-select>',
      '<p-select [multiple]="true"></p-select>',
      '<p-select [multiple]="true"><ng-template #selectedItem><hf-chip-summary [chips]="chips" />' +
        '</ng-template></p-select>',
      '</ng-template>',
    ].join(NEWLINE);

    const found = multiSelectsWithoutChipSummary('m.html', source);

    expect(found).toEqual(['m.html:2', 'm.html:3']);
  });

  it('everyListbox_UsesTheOptionListClass', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) => listboxesWithoutOptionList(path, source));

    expect(offenders).toEqual([]);
  });

  it('listboxesWithoutOptionList_WithAndWithoutTheClass_NamesFileAndLine', () => {
    const source = [
      '<p-listbox class="hf-option-list" [options]="a"></p-listbox>',
      '<p-listbox [options]="b"></p-listbox>',
    ].join(NEWLINE);

    const found = listboxesWithoutOptionList('l.html', source);

    expect(found).toEqual(['l.html:2']);
  });

  it('everyFilterControl_HasAPlaceholder', () => {
    const templates = allTemplates();

    const offenders = Object.entries(templates).flatMap(([path, source]) =>
      filterControlsWithoutPlaceholder(path, source),
    );

    expect(offenders).toEqual([]);
  });

  it('filterControlsWithoutPlaceholder_WithAControlWithoutPlaceholderInAFilter_NamesFileAndLine', () => {
    const source = [
      '<ng-template #filter let-value>',
      '<input pInputText />',
      '<input [placeholder]="key | translate" />',
      '</ng-template>',
      '<input />',
    ].join(NEWLINE);

    const found = filterControlsWithoutPlaceholder('f.html', source);

    expect(found).toEqual(['f.html:2']);
  });

  it('filterControlsWithoutPlaceholder_WithANestedTemplate_KeepsTheFilterRange', () => {
    const source = [
      '<ng-template #filter let-value>',
      '<p-select><ng-template #selectedItem></ng-template></p-select>',
      '<input [placeholder]="key | translate" />',
      '</ng-template>',
      '<input />',
    ].join(NEWLINE);

    const found = filterControlsWithoutPlaceholder('n.html', source);

    expect(found).toEqual(['n.html:2']);
  });
});
