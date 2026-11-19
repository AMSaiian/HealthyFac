import type { SetFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';

import type { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';

export enum DraftState {
  Pending = 'pending',
  Applied = 'applied',
  Dismissed = 'dismissed',
}

export enum WorkOrderDraftColumn {
  Title = 'title',
  Machine = 'machine',
  Priority = 'priority',
  Category = 'category',
  Estimate = 'estimate',
  Case = 'case',
  ProposedAt = 'proposedAt',
}

/** The columns that the backend sorts by. */
export enum WorkOrderDraftSortColumn {
  ProposedAt = 'proposedAt',
  State = 'state',
}

export interface WorkOrderDraft {
  readonly id: string;
  readonly machineId: string;
  /** The name of the machine, also of a deleted one. */
  readonly machineName: string | null;
  /** Tells that the machine is soft deleted, so the name is the last known one. */
  readonly isMachineDeleted: boolean;
  readonly caseId: string;
  readonly runId: string;
  readonly title: string;
  readonly description: string | null;
  readonly priority: WorkOrderPriority;
  readonly category: WorkOrderCategory;
  readonly estimateMinutes: number;
  readonly state: DraftState;
  readonly appliedWorkOrderId: string | null;
  readonly proposedAt: string;
  readonly rowVersion: number;
}

/** The grid of this view lists the pending drafts only. */
export interface WorkOrderDraftFilter {
  readonly states: readonly DraftState[];
}

export type WorkOrderDraftsPage = GridPage<WorkOrderDraft>;

export type WorkOrderDraftSort = GridSort<WorkOrderDraftSortColumn>;

export type WorkOrderDraftGridState = GridQueryState<WorkOrderDraftFilter, WorkOrderDraftSortColumn>;

export interface WorkOrderDraftFilterBody {
  readonly states?: SetFilterBody<DraftState>;
}

export type SearchWorkOrderDraftsBody = GridRequestBody<WorkOrderDraftFilterBody, WorkOrderDraftSortColumn>;

/** The values that the apply sends: the edited fields of the draft and the version that the user read. */
export interface ApplyWorkOrderDraftBody {
  readonly rowVersion: number;
  readonly title: string;
  readonly description: string | null;
  readonly priority: WorkOrderPriority;
  readonly category: WorkOrderCategory;
  readonly estimateMinutes: number;
}

export interface DismissWorkOrderDraftBody {
  readonly rowVersion: number;
}
