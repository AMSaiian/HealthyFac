import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { IngestApi } from '@files/ingest/ingest-api';
import { toKeySegment } from '@files/ingest/ingest-attempt.utils';
import { DraftGroupList } from '@files/ingest/summary/draft-group-list/draft-group-list';
import { IngestSummaryStore } from '@files/ingest/summary/ingest-summary.store';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

@Component({
  imports: [ActionDialog, DraftGroupList, EmptyState, LoadingSpinner, TranslatePipe],
  host: { class: 'contents' },
  providers: [
    IngestApi,
    DraftsApi,
    MetricDefinitionsApi,
    MetricUnitsApi,
    StatusDefinitionsApi,
    IngestSummaryStore,
  ],
  selector: 'hf-ingest-summary-host',
  templateUrl: './ingest-summary-host.html',
})
export class IngestSummaryHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(IngestSummaryStore);

  private readonly _translate = inject(TranslateService);

  /** The values of the warning, with the kind in the language of the user. */
  protected readonly warningValues = computed(() => {
    const values = this.store.warning()?.values ?? {};
    const kind = values['kind'];
    return typeof kind === 'string'
      ? { ...values, kind: this._translate.instant(`INGEST.WARNINGS.KINDS.${toKeySegment(kind)}`) }
      : values;
  });

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
