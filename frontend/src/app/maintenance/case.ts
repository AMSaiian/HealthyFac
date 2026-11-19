import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { SetFilterBody } from '@shared/grid/requests/grid-filters';

export enum CaseState {
  Open = 'open',
  Resolved = 'resolved',
  Closed = 'closed',
}

export enum CaseColumn {
  Machine = 'machine',
  State = 'state',
  Severity = 'severity',
  Signals = 'signals',
  LastRun = 'lastRun',
  OpenedAt = 'openedAt',
}

/** The columns that the backend sorts by. */
export enum CaseSortColumn {
  OpenedAt = 'openedAt',
  State = 'state',
}

export enum SignalSeverity {
  Warning = 'warning',
  Alarm = 'alarm',
}

export enum SignalRule {
  Threshold = 'threshold',
  Drift = 'drift',
  Trend = 'trend',
  Status = 'status',
  SilentMachine = 'silentMachine',
  UnknownMode = 'unknownMode',
}

export enum SignalState {
  Open = 'open',
  Resolved = 'resolved',
}

/** What the agent did with a signal. `drafted` and `linked` carry a work order draft or a work order. */
export enum SignalCoverage {
  New = 'new',
  Watched = 'watched',
  Dismissed = 'dismissed',
  Drafted = 'drafted',
  Linked = 'linked',
}

export enum AgentRole {
  Triage = 'triage',
  Planner = 'planner',
  Extractor = 'extractor',
  Screener = 'screener',
}

export enum AgentRunState {
  Queued = 'queued',
  Running = 'running',
  Completed = 'completed',
  Failed = 'failed',
  Interrupted = 'interrupted',
  Skipped = 'skipped',
}

/** Why a run did nothing: the case was no longer open, or no open signal needed a review. */
export enum AgentRunSkipReason {
  CaseNotOpen = 'caseNotOpen',
  NothingToReview = 'nothingToReview',
}

export enum AgentRunVerdict {
  Dismiss = 'dismiss',
  Watch = 'watch',
  Escalate = 'escalate',
  Safe = 'safe',
  Unsafe = 'unsafe',
}

export enum AgentRunErrorCode {
  ProviderUnavailable = 'providerUnavailable',
  UnexpectedError = 'unexpectedError',
  CaseChanged = 'caseChanged',
  NoDecision = 'noDecision',
}

export enum ToolCallOutcome {
  Succeeded = 'succeeded',
  Rejected = 'rejected',
  Failed = 'failed',
}

export interface CaseListItem {
  readonly id: string;
  readonly machineId: string;
  readonly state: CaseState;
  readonly highestOpenSeverity: SignalSeverity | null;
  readonly signalCount: number;
  readonly lastRunState: AgentRunState | null;
  readonly isAiUnavailable: boolean;
  readonly openedAt: string;
  readonly closedAt: string | null;
  readonly closeReason: string | null;
}

export interface CaseSignal {
  readonly id: string;
  readonly rule: SignalRule;
  readonly severity: SignalSeverity;
  readonly state: SignalState;
  readonly coverage: SignalCoverage;
  readonly metricKey: string | null;
  readonly statusCode: string | null;
  readonly value: number | null;
  readonly limit: number | null;
  readonly openedAt: string;
  readonly resolvedAt: string | null;
  readonly workOrderDraftId: string | null;
  readonly workOrderId: string | null;
}

export interface CaseToolCall {
  readonly sequence: number;
  readonly toolName: string;
  readonly outcome: ToolCallOutcome;
  readonly arguments: string;
  readonly result: string;
  readonly durationMilliseconds: number;
}

export interface CaseRun {
  readonly id: string;
  readonly role: AgentRole;
  readonly state: AgentRunState;
  readonly model: string | null;
  readonly promptVersion: string | null;
  readonly attemptNumber: number;
  readonly inputTokens: number;
  readonly outputTokens: number;
  readonly verdict: AgentRunVerdict | null;
  readonly verdictReason: string | null;
  readonly errorCode: AgentRunErrorCode | null;
  readonly skipReason: AgentRunSkipReason | null;
  readonly startedAt: string | null;
  readonly finishedAt: string | null;
}

/** What the agents decided for a case. */
export enum CaseOutcomeKind {
  ProposedWorkOrder = 'proposedWorkOrder',
  LinkedWorkOrder = 'linkedWorkOrder',
  WorkOrderCreated = 'workOrderCreated',
  ProposalDismissed = 'proposalDismissed',
  Closed = 'closed',
  Watched = 'watched',
  Dismissed = 'dismissed',
  Escalated = 'escalated',
  NoDecision = 'noDecision',
  AiUnavailable = 'aiUnavailable',
  None = 'none',
}

/** The result of a case. `reason` is the final text of the last triage or planner run, or the close reason. */
export interface CaseOutcome {
  readonly kind: CaseOutcomeKind;
  readonly workOrderDraftId: string | null;
  readonly workOrderId: string | null;
  readonly title: string | null;
  readonly reason: string | null;
}

/** The header of a case with its outcome. The signals and the runs have their own paged reads. */
export interface CaseDetail extends CaseListItem {
  readonly outcome: CaseOutcome;
}

export interface CaseToolCallsResponse {
  readonly toolCalls: readonly CaseToolCall[];
}

/** The page of a case child list, as the backend reads it. */
export interface CaseChildrenBody {
  readonly offset: number;
  readonly limit: number;
}

export type CaseSignalsPage = GridPage<CaseSignal>;

export type CaseRunsPage = GridPage<CaseRun>;

export interface CloseCaseBody {
  readonly reason: string;
}

export type CasesPage = GridPage<CaseListItem>;

export type CaseSort = GridSort<CaseSortColumn>;

export interface CaseFilter {
  readonly machineIds: readonly string[];
  readonly states: readonly CaseState[];
  readonly severities: readonly SignalSeverity[];
  /** The states of the last run of the case. An empty list lists all cases. */
  readonly lastRunStates: readonly AgentRunState[];
  /** `true` lists the cases whose AI is unavailable (together with the run states). `null` sets no limit. */
  readonly isAiUnavailable: true | null;
}

export type CaseGridState = GridQueryState<CaseFilter, CaseSortColumn>;

export interface CaseFilterBody {
  readonly machineIds?: SetFilterBody<string>;
  readonly states?: SetFilterBody<CaseState>;
  readonly severities?: SetFilterBody<SignalSeverity>;
  readonly lastRunStates?: SetFilterBody<AgentRunState>;
  readonly isAiUnavailable?: true;
}

export type SearchCasesBody = GridRequestBody<CaseFilterBody, CaseSortColumn>;
