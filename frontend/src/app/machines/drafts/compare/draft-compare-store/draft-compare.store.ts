import { signalStore } from '@ngrx/signals';

import { withToasts } from '@shared/toasts/with-toasts';

import { withDraftCompareAction } from '@machines/drafts/compare/draft-compare-store/draft-compare.action.slice';

import { withStoreDevtools } from '@shared/devtools/with-store-devtools';

/** The compare of a machine draft. The action slice owns the open compare, the choices, and the save in progress. */
export const DraftCompareStore = signalStore(
  withToasts(),
  withDraftCompareAction(),
  withStoreDevtools('draft-compare'),
);
