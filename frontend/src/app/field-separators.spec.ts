/// <reference types="vite/client" />
import {
  parseTemplate,
  TmplAstElement,
  TmplAstForLoopBlock,
  TmplAstIfBlock,
  type TmplAstNode,
  TmplAstSwitchBlock,
  TmplAstTemplate,
  TmplAstText,
} from '@angular/compiler';

/**
 * In every form and read-only view, a PrimeNG `p-divider` separates every two neighbour field groups. This spec parses
 * the templates of the forms, the views, and the detail lists, and reports each place that breaks the convention.
 */
const DIVIDER = 'p-divider';
const COMPONENT_PREFIX = 'hf-';
const NOT_FIRST = '!$first';
const FIELD_LABEL_TOKEN = 'hf-field-label';
const GRID_TOKEN = 'grid';
const FLEX_TOKEN = 'flex';
const FLEX_COLUMN_TOKENS = ['flex-col', 'flex-col-reverse'];
const GAP_TOKEN = 'gap-4';
const VERTICAL = 'vertical';
const DETAIL_LIST = 'dl';
const COMPARE_TEMPLATES = [
  './machines/drafts/compare/field-compare-list/field-compare-list.html',
  './machines/drafts/status/status-draft-review-host/status-draft-review-host.html',
];

/** The templates of the forms, the views, and the detail lists (paths under `src/app/`). */
const TEMPLATES = [
  './machines/drawer/machine-form/machine-form.html',
  './machines/drawer/machine-details/machine-details.html',
  './machines/statuses/drawer/status-definition-form/status-definition-form.html',
  './machines/statuses/drawer/status-definition-details/status-definition-details.html',
  './work-orders/drawer/work-order-form/work-order-form.html',
  './work-orders/drawer/work-order-details/work-order-details.html',
  './files/edit/file-edit-form/file-edit-form.html',
  './files/edit/file-details/file-details.html',
  './machines/phases/phase-form/phase-form.html',
  './machines/phases/phase-details/phase-details.html',
  './employees/invites/invite-dialog/invite-dialog.html',
  './employees/shifts/override-dialog/override-dialog.html',
  './employees/shifts/version-dialog/version-dialog.html',
  './files/tie/file-tie-host/file-tie-host.html',
  './invite/invite-page/invite-page.html',
  './machines/drafts/phase/phase-draft-review/phase-draft-review.html',
  './machines/drafts/status/status-draft-review-host/status-draft-review-host.html',
  './machines/metrics/metric-form/metric-form.html',
  './machines/state/machine-state-host/machine-state-host.html',
  './maintenance/detail/close-case-dialog/close-case-dialog.html',
  './work-orders/drafts/draft-apply-dialog/draft-apply-dialog.html',
  './work-orders/schedule-actions/assign-dialog/assign-dialog.html',
  './work-orders/schedule-actions/schedule-actions-host/schedule-actions-host.html',
  './machines/metrics/metric-detail/metric-detail.html',
  './machines/drafts/compare/field-compare-list/field-compare-list.html',
];

function lineOf(node: TmplAstNode): number {
  return node.sourceSpan.start.line + 1;
}

function isBlank(node: TmplAstNode): boolean {
  return node instanceof TmplAstText && node.value.trim() === '';
}

function classTokens(element: TmplAstElement): string[] {
  const attribute = element.attributes.find((candidate) => candidate.name === 'class');
  return attribute === undefined ? [] : attribute.value.split(/\s+/).filter((token) => token !== '');
}

function isDivider(node: TmplAstNode): node is TmplAstElement {
  return node instanceof TmplAstElement && node.name === DIVIDER;
}

/** The child nodes of a node, all branches of a block included. */
function childrenOf(node: TmplAstNode): TmplAstNode[] {
  if (node instanceof TmplAstElement || node instanceof TmplAstTemplate) {
    return node.children;
  }
  if (node instanceof TmplAstIfBlock) {
    return node.branches.flatMap((branch) => branch.children);
  }
  if (node instanceof TmplAstForLoopBlock) {
    return node.children;
  }
  if (node instanceof TmplAstSwitchBlock) {
    return node.groups.flatMap((group) => group.children);
  }
  return [];
}

/** Tells whether a node holds, at any depth, a field label or the work order files. */
function isFieldGroup(node: TmplAstNode, components: ReadonlySet<string>): boolean {
  if (node instanceof TmplAstElement && (components.has(node.name) || classTokens(node).includes(FIELD_LABEL_TOKEN))) {
    return true;
  }
  return childrenOf(node).some((child) => isFieldGroup(child, components));
}

function holdsFieldLabel(nodes: TmplAstNode[]): boolean {
  return nodes.some(
    (node) =>
      (node instanceof TmplAstElement && classTokens(node).includes(FIELD_LABEL_TOKEN)) ||
      holdsFieldLabel(childrenOf(node)),
  );
}

/**
 * Lists the component selectors (`hf-<name>`) whose own template `./**\/<name>/<name>.html` holds a field label: a
 * component that renders its own label is a field group.
 */
export function groupComponentsOf(templates: Record<string, string>): Set<string> {
  return new Set(
    Object.entries(templates)
      .filter(([path]) => /\/([\w-]+)\/\1\.html$/.test(path))
      .filter(([path, source]) => holdsFieldLabel(parseTemplate(source, path).nodes))
      .map(([path]) => `${COMPONENT_PREFIX}${path.split('/').at(-1)!.replace('.html', '')}`),
  );
}

let componentGroups: ReadonlySet<string> = new Set();

/**
 * The units of a child list: an `@if` without `@else` and a `@for` body are spliced into the list of their parent; any
 * other block stays one unit. Blank text is dropped.
 */
function unitsOf(nodes: TmplAstNode[]): TmplAstNode[] {
  return nodes
    .filter((node) => !isBlank(node))
    .flatMap((node) => {
      if (node instanceof TmplAstIfBlock && node.branches.length === 1) {
        return unitsOf(node.branches[0].children);
      }
      if (node instanceof TmplAstForLoopBlock) {
        return unitsOf(node.children);
      }
      return [node];
    });
}

/** The child lists below a unit, each with its parent element. */
function nestedLists(unit: TmplAstNode, parent: TmplAstElement | null): [TmplAstNode[], TmplAstElement | null][] {
  if (unit instanceof TmplAstElement) {
    return [[unit.children, unit]];
  }
  if (unit instanceof TmplAstTemplate) {
    return [[unit.children, null]];
  }
  if (unit instanceof TmplAstIfBlock) {
    return unit.branches.map((branch): [TmplAstNode[], TmplAstElement | null] => [branch.children, parent]);
  }
  if (unit instanceof TmplAstSwitchBlock) {
    return unit.groups.map((group): [TmplAstNode[], TmplAstElement | null] => [group.children, parent]);
  }
  return [];
}

interface DividerPlace {
  readonly divider: TmplAstElement;
  readonly parent: TmplAstElement | null;
}

/** Walks every child list of a template and reports the places where a divider is missing, and the dividers between field groups. */
function walk(
  nodes: TmplAstNode[],
  parent: TmplAstElement | null,
  report: { missing: number[]; places: DividerPlace[] },
  components: ReadonlySet<string>,
): void {
  const units = unitsOf(nodes);
  let hasGroupBefore = false;
  units.forEach((unit, index) => {
    if (isFieldGroup(unit, components)) {
      if (hasGroupBefore) {
        const before = units[index - 1];
        if (isDivider(before)) {
          report.places.push({ divider: before, parent });
        } else {
          report.missing.push(lineOf(unit));
        }
      }
      hasGroupBefore = true;
    }
  });
  units.forEach((unit) => nestedLists(unit, parent).forEach(([list, owner]) => walk(list, owner, report, components)));
}

function analyse(
  source: string,
  path: string,
  components: ReadonlySet<string>,
): { missing: number[]; places: DividerPlace[] } {
  const parsed = parseTemplate(source, path);
  expect(parsed.errors ?? [], path).toEqual([]);
  const report = { missing: [] as number[], places: [] as DividerPlace[] };
  walk(parsed.nodes, null, report, components);
  return report;
}

/** Lists the lines of the field groups that have no `p-divider` right before them, next to an earlier group. */
export function groupsWithoutDivider(
  source: string,
  path = 'inline.html',
  components: ReadonlySet<string> = componentGroups,
): number[] {
  return analyse(source, path, components).missing;
}

/** A row is a `grid`, or a `flex` without `flex-col` and `flex-col-reverse`. */
function isRow(element: TmplAstElement): boolean {
  const tokens = classTokens(element);
  return (
    tokens.includes(GRID_TOKEN) ||
    (tokens.includes(FLEX_TOKEN) && !FLEX_COLUMN_TOKENS.some((column) => tokens.includes(column)))
  );
}

function layoutOf(divider: TmplAstElement): string | null {
  return divider.attributes.find((attribute) => attribute.name === 'layout')?.value ?? null;
}

/** Lists the lines of the dividers between field groups with the wrong `layout`: vertical in a row parent only. */
export function dividersWithWrongLayout(
  source: string,
  path = 'inline.html',
  components: ReadonlySet<string> = componentGroups,
): number[] {
  return analyse(source, path, components)
    .places.filter(({ divider, parent }) => {
      const isInRow = parent !== null && isRow(parent);
      return isInRow ? layoutOf(divider) !== VERTICAL : layoutOf(divider) !== null;
    })
    .map(({ divider }) => lineOf(divider));
}

/** Lists the lines of the dividers between field groups whose parent element has no `gap-4`. */
export function dividersOutsideGap(
  source: string,
  path = 'inline.html',
  components: ReadonlySet<string> = componentGroups,
): number[] {
  return analyse(source, path, components)
    .places.filter(({ parent }) => parent !== null && !classTokens(parent).includes(GAP_TOKEN))
    .map(({ divider }) => lineOf(divider));
}

function isNotFirstDivider(node: TmplAstNode): boolean {
  return (
    node instanceof TmplAstIfBlock &&
    node.branches.length === 1 &&
    (node.branches[0].expression as { source?: string } | null)?.source?.replace(/\s/g, '') === NOT_FIRST &&
    node.branches[0].children.some(isDivider)
  );
}

function forLoops(nodes: TmplAstNode[]): TmplAstForLoopBlock[] {
  return nodes.flatMap((node) => [
    ...(node instanceof TmplAstForLoopBlock ? [node] : []),
    ...forLoops(childrenOf(node)),
  ]);
}

/** Lists the lines of the `@for` blocks whose body holds a field group but has no `@if (!$first)` divider before it. */
export function repeatedGroupsWithoutDivider(
  source: string,
  path = 'inline.html',
  components: ReadonlySet<string> = componentGroups,
): number[] {
  const parsed = parseTemplate(source, path);
  return forLoops(parsed.nodes)
    .filter((loop) => loop.children.some((child) => isFieldGroup(child, components)))
    .filter((loop) => {
      const body = loop.children.filter((child) => !isBlank(child));
      const groupIndex = body.findIndex((child) => isFieldGroup(child, components));
      return !body.slice(0, groupIndex).some(isNotFirstDivider);
    })
    .map(lineOf);
}

function elementsNamed(nodes: TmplAstNode[], name: string): TmplAstElement[] {
  return nodes.flatMap((node) => [
    ...(node instanceof TmplAstElement && node.name === name ? [node] : []),
    ...elementsNamed(childrenOf(node), name),
  ]);
}

function elementsWithClass(nodes: TmplAstNode[], token: string): TmplAstElement[] {
  return nodes.flatMap((node) => [
    ...(node instanceof TmplAstElement && classTokens(node).includes(token) ? [node] : []),
    ...elementsWithClass(childrenOf(node), token),
  ]);
}

/** Lists the lines of the neighbour children of an element with no divider between them. */
function neighboursWithoutDivider(element: TmplAstElement): number[] {
  const units = unitsOf(element.children);
  return units.flatMap((unit, index) =>
    index > 0 && !isDivider(unit) && !isDivider(units[index - 1]) ? [lineOf(unit)] : [],
  );
}

/** Lists the lines of the rows of a `dl` that have no `p-divider` between them. */
export function detailRowsWithoutDivider(source: string, path = 'inline.html'): number[] {
  const parsed = parseTemplate(source, path);
  return elementsNamed(parsed.nodes, DETAIL_LIST).flatMap(neighboursWithoutDivider);
}

/** Lists the lines of the columns of a draft compare row with no vertical `p-divider` between them. */
export function compareColumnsWithoutDivider(source: string, path = 'inline.html'): number[] {
  const parsed = parseTemplate(source, path);
  return elementsWithClass(parsed.nodes, GRID_TOKEN).flatMap((grid) => [
    ...neighboursWithoutDivider(grid),
    ...unitsOf(grid.children)
      .filter(isDivider)
      .filter((divider) => layoutOf(divider) !== VERTICAL)
      .map(lineOf),
  ]);
}

const LABEL_GROUP = '<div><i class="hf-field-label"></i></div>';

describe('field separators', { tags: ['Conventions'] }, () => {
  const templates = import.meta.glob('./**/*.html', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  componentGroups = groupComponentsOf(templates);

  function offenders(find: (source: string, path: string) => number[], paths: readonly string[] = TEMPLATES): string[] {
    return paths.flatMap((path) => {
      expect(templates[path], path).toBeDefined();
      return find(templates[path], path).map((line) => `${path}:${line}`);
    });
  }

  it('fieldSeparators_NeighbourFieldGroups_HaveADividerBetween', () => {
    expect(Object.keys(templates).length).toBeGreaterThan(0);

    expect(offenders(groupsWithoutDivider)).toEqual([]);
  });

  it('fieldSeparators_GridColumns_HaveAVerticalDivider', () => {
    expect(offenders(dividersWithWrongLayout)).toEqual([]);
    expect(offenders(compareColumnsWithoutDivider, COMPARE_TEMPLATES)).toEqual([]);
  });

  it('fieldSeparators_RepeatedFieldGroups_HaveADividerBetween', () => {
    expect(offenders(repeatedGroupsWithoutDivider)).toEqual([]);
  });

  it('fieldSeparators_SeparatedGroups_SitInAGap4Parent', () => {
    expect(offenders(dividersOutsideGap)).toEqual([]);
  });

  it('fieldSeparators_DetailListRows_HaveADividerBetween', () => {
    expect(offenders(detailRowsWithoutDivider)).toEqual([]);
  });

  it('groupsWithoutDivider_WithTwoNeighbourGroups_ReportsTheSecond', () => {
    const source =
      '<div><label class="hf-field-label">A</label></div>\n<div><label class="hf-field-label">B</label></div>';

    expect(groupsWithoutDivider(source)).toEqual([2]);
  });

  it('groupsWithoutDivider_WithADividerAndAnIfBlock_ReportsNothing', () => {
    const source =
      '<div><label class="hf-field-label">A</label></div>\n@if (x) {\n<p-divider />\n<div><label class="hf-field-label">B</label></div>\n}';

    expect(groupsWithoutDivider(source)).toEqual([]);
  });

  it.each([
    ['AVerticalDividerOutsideAGrid', '<section class="gap-4">', '<p-divider layout="vertical" />', '</section>'],
    ['AHorizontalDividerInAFlexRow', '<div class="flex gap-4">', '<p-divider />', '</div>'],
  ])('dividersWithWrongLayout_With%s_ReportsIt', (_name, open, divider, close) => {
    const source = `${open}${LABEL_GROUP}${divider}${LABEL_GROUP}${close}`;

    expect(dividersWithWrongLayout(source)).toEqual([1]);
  });

  it.each([
    ['AVerticalDividerInAGrid', '<section class="grid gap-4">', '</section>'],
    ['AVerticalDividerInAFlexRow', '<div class="flex gap-4">', '</div>'],
  ])('dividersWithWrongLayout_With%s_ReportsNothing', (_name, open, close) => {
    const source = `${open}${LABEL_GROUP}<p-divider layout="vertical" />${LABEL_GROUP}${close}`;

    expect(dividersWithWrongLayout(source)).toEqual([]);
  });

  it('dividersOutsideGap_WithoutGap4_ReportsTheDivider', () => {
    const source =
      '<section class="gap-2"><div><i class="hf-field-label"></i></div><p-divider /><div><i class="hf-field-label"></i></div></section>';

    expect(dividersOutsideGap(source)).toEqual([1]);
  });

  it('dividersOutsideGap_AtTheTopLevel_ReportsNothing', () => {
    const source = '<div><i class="hf-field-label"></i></div><p-divider /><div><i class="hf-field-label"></i></div>';

    expect(dividersOutsideGap(source)).toEqual([]);
  });

  it('detailRowsWithoutDivider_WithTwoNeighbourRows_ReportsTheSecond', () => {
    expect(detailRowsWithoutDivider('<dl>\n<div>a</div>\n<div>b</div>\n</dl>')).toEqual([3]);
  });

  it('detailRowsWithoutDivider_WithADividerBetween_ReportsNothing', () => {
    expect(detailRowsWithoutDivider('<dl><div>a</div><p-divider /><div>b</div></dl>')).toEqual([]);
  });

  it('compareColumnsWithoutDivider_WithTwoColumnsAndNoDivider_ReportsTheSecond', () => {
    expect(compareColumnsWithoutDivider('<div class="grid">\n<div>a</div>\n<div>b</div>\n</div>')).toEqual([3]);
  });

  it('groupComponentsOf_WithALabelledComponentTemplate_ListsItsSelector', () => {
    const map = {
      './x/block-list/block-list.html': '<span class="hf-field-label">{{ label() }}</span>',
      './x/plain/plain.html': '<span>text</span>',
    };

    expect([...groupComponentsOf(map)]).toEqual(['hf-block-list']);
  });

  it('groupsWithoutDivider_WithAComponentGroup_ReportsTheMissingDivider', () => {
    const source = '<div><i class="hf-field-label"></i></div>\n<hf-block-list />';

    expect(groupsWithoutDivider(source, 'inline.html', new Set(['hf-block-list']))).toEqual([2]);
    expect(groupsWithoutDivider(source, 'inline.html', new Set())).toEqual([]);
  });

  it('repeatedGroupsWithoutDivider_WithoutTheFirstGuard_ReportsTheLoop', () => {
    const source = '@for (d of days; track d) {\n<div><i class="hf-field-label"></i></div>\n}';

    expect(repeatedGroupsWithoutDivider(source)).toEqual([1]);
  });

  it('repeatedGroupsWithoutDivider_WithTheFirstGuard_ReportsNothing', () => {
    const source =
      '@for (d of days; track d) {\n@if (!$first) {\n<p-divider />\n}\n<div><i class="hf-field-label"></i></div>\n}';

    expect(repeatedGroupsWithoutDivider(source)).toEqual([]);
  });
});
