import { type AttemptDraftGroup, DraftKind, DraftState } from '@machines/drafts/draft';

export interface DraftRow {
  readonly id: string;
  readonly kind: DraftKind;
  readonly state: DraftState;
  readonly isCreate: boolean;
  readonly label: string | null;
  readonly sourceLocation: string | null;
  readonly isPending: boolean;
  /** The machine that the draft targets, or `null` while the create draft is pending. */
  readonly machineId: string | null;
  /** A child of a pending create draft waits until the machine exists. */
  readonly isReviewDisabled: boolean;
}

export interface DraftGroupView {
  readonly key: string;
  readonly machineDraftId: string | null;
  readonly machineId: string | null;
  readonly hasPending: boolean;
  readonly titleKey: string;
  readonly titleName: string;
  readonly rows: readonly DraftRow[];
}

const TITLE_MACHINE = 'DRAFTS.GROUP.MACHINE';
const TITLE_NEW_MACHINE = 'DRAFTS.GROUP.NEW_MACHINE';
const TITLE_NEW_MACHINE_UNNAMED = 'DRAFTS.GROUP.NEW_MACHINE_UNNAMED';
const TITLE_UNKNOWN = 'DRAFTS.GROUP.UNKNOWN';

function titleOf(group: AttemptDraftGroup): Pick<DraftGroupView, 'titleKey' | 'titleName'> {
  const create = group.machineDraft?.isCreate === true ? group.machineDraft : null;
  if (create !== null) {
    return create.name === null
      ? { titleKey: TITLE_NEW_MACHINE_UNNAMED, titleName: '' }
      : { titleKey: TITLE_NEW_MACHINE, titleName: create.name };
  }
  if (group.machineName !== null) {
    return { titleKey: TITLE_MACHINE, titleName: group.machineName };
  }
  return { titleKey: TITLE_UNKNOWN, titleName: '' };
}

/**
 * Groups the drafts of one attempt by machine for the summary. A child draft of a pending create draft is in the group
 * of that create draft, and its review is disabled.
 */
export function toDraftGroups(groups: readonly AttemptDraftGroup[]): DraftGroupView[] {
  return groups.map((group, index) => {
    const isCreatePending = group.machineDraft?.isCreate === true && group.machineDraft.state === DraftState.Pending;
    const machineRows: DraftRow[] =
      group.machineDraft === null
        ? []
        : [
            {
              id: group.machineDraft.id,
              kind: DraftKind.Machine,
              state: group.machineDraft.state,
              isCreate: group.machineDraft.isCreate,
              label: group.machineDraft.name,
              sourceLocation: null,
              isPending: group.machineDraft.state === DraftState.Pending,
              machineId: group.machineId,
              isReviewDisabled: false,
            },
          ];
    const childRows: DraftRow[] = [
      ...group.metrics.map((metric) => ({
        id: metric.id,
        kind: DraftKind.Metric,
        state: metric.state,
        isCreate: false,
        label: metric.name ?? metric.key,
        sourceLocation: metric.sourceLocation,
        isPending: metric.state === DraftState.Pending,
        machineId: group.machineId,
        isReviewDisabled: isCreatePending || group.machineId === null,
      })),
      ...group.statuses.map((status) => ({
        id: status.id,
        kind: DraftKind.Status,
        state: status.state,
        isCreate: false,
        label: status.code,
        sourceLocation: status.sourceLocation,
        isPending: status.state === DraftState.Pending,
        machineId: group.machineId,
        isReviewDisabled: isCreatePending || group.machineId === null,
      })),
    ];
    return {
      key: group.machineDraft?.id ?? group.machineId ?? `group-${index}`,
      machineDraftId: group.machineDraft?.id ?? null,
      machineId: group.machineId,
      hasPending: [...machineRows, ...childRows].some((row) => row.isPending),
      ...titleOf(group),
      rows: [...machineRows, ...childRows],
    };
  });
}
