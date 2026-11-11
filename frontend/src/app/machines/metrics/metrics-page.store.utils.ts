import { safeParse } from 'zod/mini';

import { hasFormChanges } from '@shared/forms/form-changes';
import { toKeySlug } from '@shared/text/slugify';

import type { MetricDraftDetail } from '@machines/drafts/draft';
import {
  type ApplyMetricDefinitionsBody,
  type CreatedMetricDefinition,
  type MetricDefinition,
  type MetricUnitReference,
  type MetricUnitsResponse,
  MetricValueKind,
  type UpdatedMetricDefinition,
} from '@machines/metrics/metric-definition';
import { metricFormSchema, type MetricFormValue } from '@machines/metrics/metric-form/metric-form-schema';

const UNIT_SEPARATOR = '|';
const NEW_ITEM_PREFIX = 'new-';
const SERVER_ERROR_PATH = /^(created|updated|deleted)\[(\d+)\]/i;

/** One row of the list: a saved definition or a new one, with the value that the form staged for it. */
export interface MetricItem {
  readonly localId: string;
  readonly savedId: string | null;
  readonly saved: MetricFormValue | null;
  readonly value: MetricFormValue;
  readonly isDeleted: boolean;
  /** The metric draft that proposed this staged value, or `null` for a change of the user. */
  readonly draftId: string | null;
  readonly sourceLocation: string | null;
}

export interface MetricChanges {
  readonly created: readonly MetricItem[];
  readonly updated: readonly MetricItem[];
  readonly deleted: readonly MetricItem[];
}

export const EMPTY_METRIC_VALUE: MetricFormValue = {
  key: '',
  name: '',
  description: null,
  valueKind: MetricValueKind.Numeric,
  unit: '',
  min: null,
  max: null,
  states: [],
};

export function unitValue(unit: MetricUnitReference | null): string {
  return unit === null ? '' : `${unit.quantityKind}${UNIT_SEPARATOR}${unit.name}`;
}

export function parseUnitValue(value: string): MetricUnitReference | null {
  const index = value.indexOf(UNIT_SEPARATOR);
  return index < 0 ? null : { quantityKind: value.slice(0, index), name: value.slice(index + 1) };
}

export function toFormValue(definition: MetricDefinition): MetricFormValue {
  return {
    key: definition.key,
    name: definition.name,
    description: definition.description,
    valueKind: definition.valueKind,
    unit: unitValue(definition.unit),
    min: definition.min,
    max: definition.max,
    states: [...(definition.states ?? [])],
  };
}

export function itemsFromDefinitions(definitions: readonly MetricDefinition[]): readonly MetricItem[] {
  return definitions.map((definition) => {
    const value = toFormValue(definition);
    return {
      localId: definition.id,
      savedId: definition.id,
      saved: value,
      value,
      isDeleted: false,
      draftId: null,
      sourceLocation: null,
    };
  });
}

export function newLocalId(index: number): string {
  return `${NEW_ITEM_PREFIX}${index}`;
}

export function isNewItem(item: MetricItem): boolean {
  return item.savedId === null;
}

/** Keeps net changes only: a new item that was deleted, an edit back to the saved value, and a restored delete drop out. */
export function netChanges(items: readonly MetricItem[]): MetricChanges {
  return {
    created: items.filter((item) => isNewItem(item) && !item.isDeleted),
    updated: items.filter(
      (item) => item.saved !== null && !item.isDeleted && hasFormChanges(metricFormSchema, item.saved, item.value),
    ),
    deleted: items.filter((item) => item.savedId !== null && item.isDeleted),
  };
}

export function hasNetChanges(items: readonly MetricItem[]): boolean {
  const changes = netChanges(items);
  return changes.created.length + changes.updated.length + changes.deleted.length > 0;
}

function isNumeric(value: MetricFormValue): boolean {
  return value.valueKind === MetricValueKind.Numeric;
}

function isStateSet(value: MetricFormValue): boolean {
  return value.valueKind === MetricValueKind.StateSet;
}

function toCreated(value: MetricFormValue): CreatedMetricDefinition {
  return {
    key: value.key,
    name: value.name.trim(),
    description: value.description,
    valueKind: value.valueKind,
    unit: isNumeric(value) ? parseUnitValue(value.unit) : null,
    min: isNumeric(value) ? value.min : null,
    max: isNumeric(value) ? value.max : null,
    states: isStateSet(value) ? value.states : null,
  };
}

function toUpdated(item: MetricItem): UpdatedMetricDefinition {
  return {
    id: item.savedId ?? '',
    name: item.value.name.trim(),
    description: item.value.description,
    min: isNumeric(item.value) ? item.value.min : null,
    max: isNumeric(item.value) ? item.value.max : null,
    states: isStateSet(item.value) ? item.value.states : null,
  };
}

export function toApplyBody(expectedRevision: number, changes: MetricChanges): ApplyMetricDefinitionsBody {
  return {
    expectedRevision,
    created: changes.created.map((item) => toCreated(item.value)),
    updated: changes.updated.map(toUpdated),
    deleted: changes.deleted.map((item) => item.savedId ?? ''),
  };
}

/**
 * Maps the paths of a `400` (e.g., `Created[2].Key`) to the items that the request was built from. The index points
 * into the list of the request, so the changes of the same request resolve it. A path outside the lists is dropped.
 */
export function mapServerErrors(
  errors: Readonly<Record<string, readonly string[]>>,
  changes: MetricChanges,
): Readonly<Record<string, readonly string[]>> {
  const result: Record<string, string[]> = {};
  for (const [path, messages] of Object.entries(errors)) {
    const match = SERVER_ERROR_PATH.exec(path);
    if (match === null) {
      continue;
    }
    const group = match[1].toLowerCase() as keyof MetricChanges;
    const item = changes[group][Number(match[2])] as MetricItem | undefined;
    if (item !== undefined) {
      result[item.localId] = [...(result[item.localId] ?? []), ...messages];
    }
  }
  return result;
}

/** Tells whether the key follows the name: it is empty or equals the slug of the previous name. */
export function nextKey(previousName: string, currentKey: string, newName: string): string {
  const isFollowing = currentKey === '' || currentKey === toKeySlug(previousName);
  return isFollowing ? toKeySlug(newName) : currentKey;
}

/** The ID of the item to select after the list changes: the first item that is still there, or `null`. */
export function firstItemId(items: readonly MetricItem[]): string | null {
  return items.at(0)?.localId ?? null;
}

export enum MetricMarker {
  New = 'new',
  Edited = 'edited',
  Deleted = 'deleted',
  Invalid = 'invalid',
}

/** The state mark of a list row. An item with a server error counts as invalid before any other mark. */
export function markerOf(item: MetricItem, hasError: boolean): MetricMarker | null {
  if (hasError) {
    return MetricMarker.Invalid;
  }
  if (item.isDeleted) {
    return MetricMarker.Deleted;
  }
  if (isNewItem(item)) {
    return MetricMarker.New;
  }
  return item.saved !== null && hasFormChanges(metricFormSchema, item.saved, item.value) ? MetricMarker.Edited : null;
}

function placeholderOf(value: MetricFormValue): number | boolean | string {
  if (value.valueKind === MetricValueKind.Boolean) {
    return false;
  }
  if (value.valueKind === MetricValueKind.StateSet) {
    return value.states[0] ?? '';
  }
  return value.min ?? value.max ?? 0;
}

/**
 * Builds the sample message of a machine from the staged items of the page: every item that is not deleted, in key
 * order, with a placeholder (numeric: the minimum, else the maximum, else 0; boolean: false; state set:
 * the first state) and no statuses. A new item that the form schema rejects is left out. A list without an item has
 * no example: the result is `null`.
 */
export function buildExamplePayload(items: readonly MetricItem[], sampledAt: string): string | null {
  const values = items
    .filter((item) => !item.isDeleted && (!isNewItem(item) || safeParse(metricFormSchema, item.value).success))
    .map((item) => item.value);
  if (values.length === 0) {
    return null;
  }
  const metrics = Object.fromEntries(
    [...values]
      .sort((first, second) => (first.key < second.key ? -1 : first.key > second.key ? 1 : 0))
      .map((value) => [value.key, placeholderOf(value)]),
  );
  return JSON.stringify({ sampledAt, metrics, statuses: [] }, null, 2);
}

function parseValueKind(value: string | null): MetricValueKind {
  return (Object.values(MetricValueKind) as string[]).includes(value ?? '')
    ? (value as MetricValueKind)
    : MetricValueKind.Numeric;
}

/** Finds the unit of the catalog by the quantity kind name and the unit name (or abbreviation), ignoring the case. */
export function unitFromCatalog(
  quantityKindName: string | null,
  unitName: string | null,
  catalog: MetricUnitsResponse | undefined,
): string {
  if (quantityKindName === null || unitName === null || catalog === undefined) {
    return '';
  }
  const kind = catalog.kinds.find((candidate) => candidate.name.toLowerCase() === quantityKindName.toLowerCase());
  const wanted = unitName.toLowerCase();
  const unit = kind?.units.find(
    (candidate) => candidate.name.toLowerCase() === wanted || candidate.abbreviation.toLowerCase() === wanted,
  );
  return kind === undefined || unit === undefined ? '' : unitValue({ quantityKind: kind.name, name: unit.name });
}

export interface StagedDrafts {
  readonly items: readonly MetricItem[];
  readonly newCount: number;
}

/**
 * Stages the metric drafts of one attempt on the list. A draft whose key equals the key of an item becomes a staged
 * change of that item: only the name, the description, the limits, and the states change, and only where the draft
 * proposes a value (the key, the value kind, and the unit never change). Another draft becomes a staged new item,
 * with the unit from the catalog; a value that the draft lacks stays empty for the user.
 */
export function stageMetricDrafts(
  items: readonly MetricItem[],
  newCount: number,
  drafts: readonly MetricDraftDetail[],
  catalog: MetricUnitsResponse | undefined,
): StagedDrafts {
  let staged = [...items];
  let count = newCount;
  for (const draft of drafts) {
    const index = draft.key === null ? -1 : staged.findIndex((item) => item.value.key === draft.key);
    if (index >= 0) {
      const item = staged[index];
      staged[index] = {
        ...item,
        value: {
          ...item.value,
          name: draft.name ?? item.value.name,
          description: draft.description ?? item.value.description,
          min: draft.min ?? item.value.min,
          max: draft.max ?? item.value.max,
          states: draft.states === null ? item.value.states : [...draft.states],
        },
        draftId: draft.id,
        sourceLocation: draft.sourceLocation,
      };
    } else {
      staged = [
        ...staged,
        {
          localId: newLocalId(count),
          savedId: null,
          saved: null,
          value: {
            key: draft.key ?? '',
            name: draft.name ?? '',
            description: draft.description,
            valueKind: parseValueKind(draft.valueKind),
            unit: unitFromCatalog(draft.quantityKindName, draft.unitName, catalog),
            min: draft.min,
            max: draft.max,
            states: draft.states === null ? [] : [...draft.states],
          },
          isDeleted: false,
          draftId: draft.id,
          sourceLocation: draft.sourceLocation,
        },
      ];
      count += 1;
    }
  }
  return { items: staged, newCount: count };
}

/** The draft IDs of the staged items that an apply sends (new and changed items, not deleted ones). */
export function appliedDraftIds(changes: MetricChanges): string[] {
  return [...changes.created, ...changes.updated]
    .map((item) => item.draftId)
    .filter((draftId): draftId is string => draftId !== null);
}
