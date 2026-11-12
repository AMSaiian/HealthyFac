import { type MetricUnitsResponse, MetricValueKind } from '@machines/metrics/metric-definition';
import { unitValue } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

export const KIND_LABEL_KEYS: Readonly<Record<MetricValueKind, string>> = {
  [MetricValueKind.Numeric]: 'METRICS.KINDS.NUMERIC',
  [MetricValueKind.Boolean]: 'METRICS.KINDS.BOOLEAN',
  [MetricValueKind.StateSet]: 'METRICS.KINDS.STATE_SET',
};

export const KIND_OPTIONS: { readonly value: MetricValueKind; readonly labelKey: string }[] = Object.values(
  MetricValueKind,
).map((value) => ({ value, labelKey: KIND_LABEL_KEYS[value] }));

export interface UnitOption {
  /** The abbreviation. It is empty for a unit without one, and the view shows a translated label then. */
  readonly label: string;
  readonly value: string;
  readonly isUnitless: boolean;
}

export interface UnitGroup {
  readonly label: string;
  readonly items: UnitOption[];
}

/** Groups the catalog by quantity kind for the grouped select. A value encodes the reference (`unitValue`). */
export function toUnitGroups(response: MetricUnitsResponse | undefined): UnitGroup[] {
  return (response?.kinds ?? []).map((kind) => ({
    label: kind.name,
    items: kind.units.map((unit) => ({
      label: unit.abbreviation,
      value: unitValue({ quantityKind: kind.name, name: unit.name }),
      isUnitless: unit.abbreviation === '',
    })),
  }));
}

/** Finds the option of an encoded unit value in the grouped catalog. */
export function findUnitOption(groups: readonly UnitGroup[], value: string): UnitOption | undefined {
  return groups.flatMap((group) => group.items).find((option) => option.value === value);
}

/** The translation key of a quantity kind, e.g. `RotationalSpeed` gives `METRICS.QUANTITY_KINDS.ROTATIONAL_SPEED`. */
export function quantityKindKey(name: string): string {
  return `METRICS.QUANTITY_KINDS.${name.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toUpperCase()}`;
}
