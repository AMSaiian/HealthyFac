import { DraftKind, type MachineDraftGridItem } from '@machines/drafts/draft';

const TITLE_MACHINE = 'DRAFTS.GROUP.MACHINE';
const TITLE_NEW_MACHINE = 'DRAFTS.GROUP.NEW_MACHINE';
const TITLE_NEW_MACHINE_UNNAMED = 'DRAFTS.GROUP.NEW_MACHINE_UNNAMED';

export interface MachineTitle {
  readonly key: string;
  readonly name: string;
}

/** The title of the machine of a row: the machine, a new machine with its proposed name, or an unnamed new machine. */
export function machineTitleOf(row: MachineDraftGridItem): MachineTitle {
  if (row.machineId !== null) {
    return { key: TITLE_MACHINE, name: row.machineName ?? '' };
  }
  return row.machineName === null
    ? { key: TITLE_NEW_MACHINE_UNNAMED, name: '' }
    : { key: TITLE_NEW_MACHINE, name: row.machineName };
}

/** A child draft of a pending create draft waits until the machine exists. */
export function isReviewDisabledOf(row: MachineDraftGridItem): boolean {
  return row.kind !== DraftKind.Machine && row.machineId === null;
}
