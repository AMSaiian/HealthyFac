import { computed, effect, type Signal, signal } from '@angular/core';

/** An option of a picker or a filter: the ID of a row and the name that the picker shows. */
export interface LookupOption {
  readonly id: string;
  readonly displayName: string;
  /** Tells that the row is soft deleted. A picker never lists a deleted row, so it is `false` there. */
  readonly isDeleted: boolean;
}

/** A request for the options: the typed text, and whether it goes out at once or after the typing pause. */
export interface LookupRequest {
  readonly text: string;
  readonly isImmediate: boolean;
}

/** The pause after the last typed letter before a lookup goes to the server. */
export const LOOKUP_DEBOUNCE_MILLISECONDS = 300;

/** The selected options first, then the found options that are not selected. An ID appears once. */
export function mergeLookupOptions(selected: LookupOption[], found: LookupOption[]): LookupOption[] {
  const selectedIds = new Set(selected.map((option) => option.id));
  return [...selected, ...found.filter((option) => !selectedIds.has(option.id))];
}

/**
 * Remembers every option that `options` held, so a selected ID keeps its option object when a later lookup page does
 * not hold it. Call it in an injection context. The result is the selected options first, then the current options.
 */
export function keepSelectedOptions(
  options: Signal<LookupOption[]>,
  selectedIds: () => readonly (string | null)[],
): Signal<LookupOption[]> {
  const known = signal<Readonly<Record<string, LookupOption>>>({});
  effect(() => {
    const current = options();
    known.update((remembered) => ({
      ...remembered,
      ...Object.fromEntries(current.map((option) => [option.id, option])),
    }));
  });
  return computed(() => {
    const remembered = known();
    const selected = selectedIds()
      .map((id) => (id === null ? undefined : remembered[id]))
      .filter((option): option is LookupOption => option !== undefined);
    return mergeLookupOptions(selected, options());
  });
}

/** The name of a machine and whether it is deleted. A view shows a deleted machine with a muted suffix. */
export interface MachineLabel {
  readonly name: string;
  readonly isDeleted: boolean;
}

/**
 * The label of a machine that a view shows. A name that is not known shows `unknownName`, and an unknown machine is
 * never marked as deleted.
 */
export function machineLabelOf(name: string | null | undefined, isDeleted: boolean, unknownName: string): MachineLabel {
  return name == null ? { name: unknownName, isDeleted: false } : { name, isDeleted };
}

/** The labels by ID. An ID that the options do not hold has no entry. */
export function labelsById(options: readonly LookupOption[]): ReadonlyMap<string, MachineLabel> {
  return new Map(options.map((option) => [option.id, { name: option.displayName, isDeleted: option.isDeleted }]));
}
