import { Component, effect, inject, input } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { DraftCompareStore } from '@machines/drafts/compare/draft-compare.store';
import { FieldCompareList } from '@machines/drafts/compare/field-compare-list/field-compare-list';
import { DraftsApi } from '@machines/drafts/drafts-api';

@Component({
  imports: [ActionDialog, FieldCompareList, LoadingSpinner],
  host: { class: 'contents' },
  providers: [DraftsApi, DraftCompareStore],
  selector: 'hf-draft-compare-host',
  templateUrl: './draft-compare-host.html',
})
export class DraftCompareHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(DraftCompareStore);

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
