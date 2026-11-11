import type { CurrentMachineValues, MachineDraftDetail } from '@machines/drafts/draft';
import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

export enum CompareField {
  Name = 'name',
  SerialNumber = 'serialNumber',
  Description = 'description',
}

export const COMPARE_FIELDS: readonly CompareField[] = [
  CompareField.Name,
  CompareField.SerialNumber,
  CompareField.Description,
];

/** What the user did with the fields: which ones are taken, and the edited value of a taken one. */
export interface CompareChoices {
  readonly taken: Readonly<Partial<Record<CompareField, boolean>>>;
  readonly edited: Readonly<Partial<Record<CompareField, string | null>>>;
}

export const NO_CHOICES: CompareChoices = { taken: {}, edited: {} };

export interface CompareRow {
  readonly field: CompareField;
  readonly current: string | null;
  /** `null` means "no change": the model proposed nothing for this field. */
  readonly proposed: string | null;
  readonly canTake: boolean;
  readonly isTaken: boolean;
  /** The value that the row gives to the merge when it is taken: the edited value, else the proposed one. */
  readonly value: string | null;
}

function proposedOf(draft: MachineDraftDetail, field: CompareField): string | null {
  return draft[field];
}

function currentOf(current: CurrentMachineValues, field: CompareField): string | null {
  return current[field];
}

/** Builds one row per field. A field with a `null` proposal cannot be taken. */
export function toCompareRows(
  draft: MachineDraftDetail,
  current: CurrentMachineValues,
  choices: CompareChoices,
): CompareRow[] {
  return COMPARE_FIELDS.map((field) => {
    const proposed = proposedOf(draft, field);
    const canTake = proposed !== null;
    const edited = choices.edited[field];
    return {
      field,
      current: currentOf(current, field),
      proposed,
      canTake,
      isTaken: canTake && choices.taken[field] === true,
      value: edited === undefined ? proposed : edited,
    };
  });
}

/** Merges the current values with the taken fields. An untaken field keeps its current value. */
export function mergeValues(current: CurrentMachineValues, rows: readonly CompareRow[]): MachineFormValue {
  const merged: MachineFormValue = {
    name: current.name,
    serialNumber: current.serialNumber,
    description: current.description,
  };
  return rows.reduce<MachineFormValue>(
    (result, row) =>
      row.isTaken
        ? row.field === CompareField.Description
          ? { ...result, description: row.value }
          : { ...result, [row.field]: row.value ?? '' }
        : result,
    merged,
  );
}

export function toCurrentFormValue(current: CurrentMachineValues): MachineFormValue {
  return { name: current.name, serialNumber: current.serialNumber, description: current.description };
}
