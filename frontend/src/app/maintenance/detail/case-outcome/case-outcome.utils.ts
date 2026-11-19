import { type CaseOutcome, CaseOutcomeKind } from '@maintenance/case';

/** Where the link of the outcome card leads. */
export enum OutcomeLink {
  Draft = 'draft',
  WorkOrder = 'workOrder',
  RunAgain = 'runAgain',
}

export interface OutcomeView {
  /** The translation key of the sentence. */
  readonly sentenceKey: string;
  /** The parameters of the sentence. */
  readonly params: Readonly<Record<string, string>>;
  readonly link: OutcomeLink | null;
  /** The final text of the agent: shown as "Why". Empty when the run wrote none. */
  readonly why: string | null;
}

const SENTENCE_KEYS: Readonly<Record<CaseOutcomeKind, string>> = {
  [CaseOutcomeKind.ProposedWorkOrder]: 'MAINTENANCE.OUTCOME.PROPOSED_WORK_ORDER',
  [CaseOutcomeKind.LinkedWorkOrder]: 'MAINTENANCE.OUTCOME.LINKED_WORK_ORDER',
  [CaseOutcomeKind.WorkOrderCreated]: 'MAINTENANCE.OUTCOME.WORK_ORDER_CREATED',
  [CaseOutcomeKind.ProposalDismissed]: 'MAINTENANCE.OUTCOME.PROPOSAL_DISMISSED',
  [CaseOutcomeKind.Closed]: 'MAINTENANCE.OUTCOME.CLOSED',
  [CaseOutcomeKind.Watched]: 'MAINTENANCE.OUTCOME.WATCHED',
  [CaseOutcomeKind.Dismissed]: 'MAINTENANCE.OUTCOME.DISMISSED',
  [CaseOutcomeKind.Escalated]: 'MAINTENANCE.OUTCOME.ESCALATED',
  [CaseOutcomeKind.NoDecision]: 'MAINTENANCE.OUTCOME.NO_DECISION',
  [CaseOutcomeKind.AiUnavailable]: 'MAINTENANCE.OUTCOME.AI_UNAVAILABLE',
  [CaseOutcomeKind.None]: 'MAINTENANCE.OUTCOME.NONE',
};

/**
 * The sentence, the link, and the "Why" text of the outcome card. A closed case shows its close reason in the sentence,
 * so it has no separate "Why". `canRunAgain` is true when the case is open and the user may write.
 */
export function outcomeView(outcome: CaseOutcome, canRunAgain: boolean): OutcomeView {
  const isClosed = outcome.kind === CaseOutcomeKind.Closed;
  return {
    sentenceKey: SENTENCE_KEYS[outcome.kind],
    params: { title: outcome.title ?? '', reason: isClosed ? (outcome.reason ?? '') : '' },
    link: linkOf(outcome, canRunAgain),
    why: isClosed || !outcome.reason ? null : outcome.reason,
  };
}

function linkOf(outcome: CaseOutcome, canRunAgain: boolean): OutcomeLink | null {
  switch (outcome.kind) {
    case CaseOutcomeKind.ProposedWorkOrder:
      return OutcomeLink.Draft;
    case CaseOutcomeKind.LinkedWorkOrder:
    case CaseOutcomeKind.WorkOrderCreated:
      return OutcomeLink.WorkOrder;
    case CaseOutcomeKind.AiUnavailable:
    case CaseOutcomeKind.NoDecision:
      return canRunAgain ? OutcomeLink.RunAgain : null;
    default:
      return null;
  }
}
