import { Component, computed, effect, inject, input } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { DraftsApi } from '@machines/drafts/drafts-api';
import { PhaseDraftReview } from '@machines/drafts/phase/phase-draft-review/phase-draft-review';
import { PhaseDraftReviewStore } from '@machines/drafts/phase/phase-draft-review-store/phase-draft-review.store';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

@Component({
  imports: [ActionDialog, LoadingSpinner, PhaseDraftReview],
  host: { class: 'contents' },
  providers: [DraftsApi, MetricDefinitionsApi, StatusDefinitionsApi, PhaseDraftReviewStore],
  selector: 'hf-phase-draft-review-host',
  templateUrl: './phase-draft-review-host.html',
})
export class PhaseDraftReviewHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(PhaseDraftReviewStore);
  protected readonly showSpinner = delayedLoading(computed(() => this.store.isLoading()));

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
