import type { SetFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';

export enum DraftKind {
  Machine = 'machine',
  Metric = 'metric',
  Status = 'status',
}

export enum DraftState {
  Pending = 'pending',
  Applied = 'applied',
  Dismissed = 'dismissed',
}

/** The columns of the drafts grid of a machine. */
export enum DraftColumn {
  Kind = 'kind',
  Label = 'label',
  SourceLocation = 'sourceLocation',
  ProposedAt = 'proposedAt',
}

/** The column that the backend sorts the drafts of a machine by. */
export enum DraftSortColumn {
  ProposedAt = 'proposedAt',
}

export interface MachineDraftDetail {
  readonly id: string;
  readonly state: DraftState;
  readonly isCreate: boolean;
  readonly attemptId: string;
  readonly fileId: string;
  readonly name: string | null;
  readonly serialNumber: string | null;
  readonly description: string | null;
  readonly rowVersion: number;
}

export interface MetricDraftDetail {
  readonly id: string;
  readonly state: DraftState;
  readonly sourceLocation: string | null;
  readonly key: string | null;
  readonly name: string | null;
  readonly description: string | null;
  readonly valueKind: string | null;
  readonly quantityKindName: string | null;
  readonly unitName: string | null;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
}

export interface StatusDraftDetail {
  readonly id: string;
  readonly state: DraftState;
  readonly sourceLocation: string | null;
  readonly code: string | null;
  readonly severity: string | null;
  readonly description: string | null;
}

/** The drafts of one machine in one attempt. A group of a create draft has no machine ID yet. */
export interface AttemptDraftGroup {
  readonly machineId: string | null;
  readonly machineName: string | null;
  readonly machineDraft: MachineDraftDetail | null;
  readonly metrics: readonly MetricDraftDetail[];
  readonly statuses: readonly StatusDraftDetail[];
}

/** The answer of the call that dismisses every pending draft of a machine. */
export interface DismissMachineDraftsResponse {
  readonly dismissedCount: number;
}

export interface AttemptDrafts {
  readonly groups: readonly AttemptDraftGroup[];
}

export interface CurrentMachineValues {
  readonly machineId: string;
  readonly name: string;
  readonly serialNumber: string;
  readonly description: string | null;
  readonly rowVersion: number;
}

export interface MachineDraftResponse {
  readonly draft: MachineDraftDetail;
  readonly current: CurrentMachineValues | null;
}

export interface ApplyMachineDraftBody {
  readonly name: string;
  readonly serialNumber: string;
  readonly description: string | null;
  readonly rowVersion?: number;
}

export interface ApplyMachineDraftResponse {
  readonly machineId: string;
}

/** One row of the drafts grid of a machine. */
export interface MachineDraftGridItem {
  readonly id: string;
  readonly kind: DraftKind;
  readonly attemptId: string;
  readonly fileId: string;
  readonly proposedAt: string;
  readonly sourceLocation: string | null;
  readonly label: string | null;
  readonly rowVersion: number;
}

export type MachineDraftsPage = GridPage<MachineDraftGridItem>;

export type MachineDraftSort = GridSort<DraftSortColumn>;

export interface MachineDraftFilter {
  readonly kinds: readonly DraftKind[];
}

export type MachineDraftGridState = GridQueryState<MachineDraftFilter, DraftSortColumn>;

export interface MachineDraftFilterBody {
  readonly kinds?: SetFilterBody<DraftKind>;
}

export type SearchMachineDraftsBody = GridRequestBody<MachineDraftFilterBody, DraftSortColumn>;
