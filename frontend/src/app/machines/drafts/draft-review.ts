import type { Router } from '@angular/router';

import type { AppAction } from '@shared/actions/app-action';

import { draftScopeOf, type DraftScopeRef } from '@machines/drafts/draft';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachineActionType } from '@machines/machine-actions';

/**
 * Opens the review of one status draft. The dialog creates the status when the machine has no status with the code,
 * else it compares the draft with that status.
 */
export function reviewStatusDraft(
  actions: { dispatchChild(action: AppAction): Promise<void> },
  target: { machineId: string; draftId: string } & DraftScopeRef,
): void {
  void actions.dispatchChild({ type: DraftActionType.ReviewStatus, payload: target });
}

/** Opens the review of one phase draft. The dialog shows its conditions and limit rows, and applies it. */
export function reviewPhaseDraft(
  actions: { dispatchChild(action: AppAction): Promise<void> },
  target: { machineId: string; draftId: string } & DraftScopeRef,
): void {
  void actions.dispatchChild({ type: DraftActionType.ReviewPhase, payload: target });
}

/**
 * Opens the review of one machine draft. A create draft opens the machine drawer with the draft values. An update
 * draft opens the compare dialog.
 */
export function reviewMachineDraft(
  actions: { dispatchChild(action: AppAction): Promise<void> },
  draftId: string,
  isCreate: boolean,
): void {
  void actions.dispatchChild(
    isCreate
      ? { type: MachineActionType.Create, payload: { draftId } }
      : { type: DraftActionType.CompareMachine, payload: { draftId } },
  );
}

const MACHINES_PATH = '/machines';
const METRICS_PATH = 'metrics';
const DRAFT_ATTEMPT_PARAM = 'draftAttemptId';
const DRAFT_CHAT_PARAM = 'draftChatId';

/**
 * Opens the metrics page of a machine with the pending metric drafts of one attempt or chat staged as proposed items.
 * The page reads the attempt from the query parameter <c>draftAttemptId</c>, or the chat from <c>draftChatId</c>. The
 * navigation adds a history entry and keeps the open dialog in the entry it leaves, so Back opens the dialog again.
 */
export function reviewMetricDrafts(router: Router, machineId: string, scope: DraftScopeRef): Promise<boolean> {
  const proposalScope = draftScopeOf(scope);
  if (proposalScope === undefined) {
    return Promise.resolve(false);
  }
  const queryParams =
    proposalScope.kind === 'attempt'
      ? { [DRAFT_ATTEMPT_PARAM]: proposalScope.id }
      : { [DRAFT_CHAT_PARAM]: proposalScope.id };
  return router.navigate([MACHINES_PATH, machineId, METRICS_PATH], { queryParams });
}
