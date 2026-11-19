import { type CaseOutcome, CaseOutcomeKind } from '@maintenance/case';
import { OutcomeLink, outcomeView } from '@maintenance/detail/case-outcome/case-outcome.utils';

function outcome(kind: CaseOutcomeKind, extra: Partial<CaseOutcome> = {}): CaseOutcome {
  return { kind, workOrderDraftId: null, workOrderId: null, title: null, reason: null, ...extra };
}

describe('case outcome utils', () => {
  it.each([
    [CaseOutcomeKind.ProposedWorkOrder, 'MAINTENANCE.OUTCOME.PROPOSED_WORK_ORDER', OutcomeLink.Draft],
    [CaseOutcomeKind.LinkedWorkOrder, 'MAINTENANCE.OUTCOME.LINKED_WORK_ORDER', OutcomeLink.WorkOrder],
    [CaseOutcomeKind.WorkOrderCreated, 'MAINTENANCE.OUTCOME.WORK_ORDER_CREATED', OutcomeLink.WorkOrder],
    [CaseOutcomeKind.ProposalDismissed, 'MAINTENANCE.OUTCOME.PROPOSAL_DISMISSED', null],
    [CaseOutcomeKind.Closed, 'MAINTENANCE.OUTCOME.CLOSED', null],
    [CaseOutcomeKind.Watched, 'MAINTENANCE.OUTCOME.WATCHED', null],
    [CaseOutcomeKind.Dismissed, 'MAINTENANCE.OUTCOME.DISMISSED', null],
    [CaseOutcomeKind.Escalated, 'MAINTENANCE.OUTCOME.ESCALATED', null],
    [CaseOutcomeKind.NoDecision, 'MAINTENANCE.OUTCOME.NO_DECISION', OutcomeLink.RunAgain],
    [CaseOutcomeKind.AiUnavailable, 'MAINTENANCE.OUTCOME.AI_UNAVAILABLE', OutcomeLink.RunAgain],
    [CaseOutcomeKind.None, 'MAINTENANCE.OUTCOME.NONE', null],
  ])('outcomeView_Of%s_GivesTheSentenceAndTheLink', (kind, sentenceKey, link) => {
    const view = outcomeView(outcome(kind), true);

    expect(view.sentenceKey).toBe(sentenceKey);
    expect(view.link).toBe(link);
  });

  it.each([CaseOutcomeKind.NoDecision, CaseOutcomeKind.AiUnavailable])(
    'outcomeView_Of%sWithoutWriteRights_HasNoRunAgainLink',
    (kind) => {
      expect(outcomeView(outcome(kind), false).link).toBeNull();
    },
  );

  it('outcomeView_OfAProposal_PassesTheTitleAndTheFinalTextAsWhy', () => {
    const view = outcomeView(
      outcome(CaseOutcomeKind.ProposedWorkOrder, { title: 'Replace the filter', reason: 'The pressure rises.' }),
      true,
    );

    expect(view.params['title']).toBe('Replace the filter');
    expect(view.why).toBe('The pressure rises.');
  });

  it('outcomeView_OfAClosedCase_PutsTheCloseReasonInTheSentenceAndHasNoWhy', () => {
    const view = outcomeView(outcome(CaseOutcomeKind.Closed, { reason: 'Fixed by the operator' }), true);

    expect(view.params['reason']).toBe('Fixed by the operator');
    expect(view.why).toBeNull();
  });
});
