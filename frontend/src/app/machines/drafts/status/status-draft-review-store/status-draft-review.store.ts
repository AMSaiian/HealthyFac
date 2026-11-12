import { signalStore } from '@ngrx/signals';

import { withToasts } from '@shared/toasts/with-toasts';

import { withStatusDraftAction } from '@machines/drafts/status/status-draft-review-store/status-draft-review.action.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/** The status review. The action slice owns the open review, the fields that the user takes, and the save in progress. */
export const StatusDraftReviewStore = signalStore(
  withToasts(),
  withStatusDraftAction(),
  withStoreDevtools('status-draft-review'),
);
