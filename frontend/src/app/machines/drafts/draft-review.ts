import type { Router } from '@angular/router';

import type { AppAction } from '@shared/actions/app-action';

import { DraftActionType } from '@machines/drafts/draft-actions';
import { MachineActionType } from '@machines/machine-actions';

/**
 * Opens the review of one status draft. The dialog creates the status when the machine has no status with the code,
 * else it compares the draft with that status.
 */
export function reviewStatusDraft(
  actions: { dispatchChild(action: AppAction): Promise<void> },
  target: { machineId: string; attemptId: string; draftId: string },
): void {
  void actions.dispatchChild({ type: DraftActionType.ReviewStatus, payload: target });
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

/** The part of the action store that the metrics review uses to keep the dialog and to close it. */
interface ReturnPathActions {
  active(): AppAction | null;
  cancel(): void;
  rememberReturn(url: string, action: AppAction, targetPrefix: string): void;
}

const MACHINES_PATH = '/machines';
const METRICS_PATH = 'metrics';
const DRAFT_ATTEMPT_PARAM = 'draftAttemptId';

/**
 * Opens the metrics page of a machine with the pending metric drafts of one attempt staged as proposed items. The
 * page reads the attempt from the query parameter <c>draftAttemptId</c>. The open dialog closes, and the action store
 * keeps it with the current URL, so it opens again when the router comes back to that URL.
 */
export function reviewMetricDrafts(
  actions: ReturnPathActions,
  router: Router,
  machineId: string,
  attemptId: string,
): Promise<boolean> {
  const dialog = actions.active();
  if (dialog !== null) {
    actions.rememberReturn(router.url, dialog, `${MACHINES_PATH}/${machineId}/${METRICS_PATH}`);
  }
  actions.cancel();
  return router.navigate([MACHINES_PATH, machineId, METRICS_PATH], {
    queryParams: { [DRAFT_ATTEMPT_PARAM]: attemptId },
  });
}
