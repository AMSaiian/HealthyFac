import { Component, computed, effect, inject, input } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { DraftCompareStore } from '@machines/drafts/compare/draft-compare-store/draft-compare.store';
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
  protected readonly isWaiting = computed(() => this.store.data.isLoading() || this.store.rows().length === 0);
  protected readonly showSpinner = delayedLoading(this.isWaiting);

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
