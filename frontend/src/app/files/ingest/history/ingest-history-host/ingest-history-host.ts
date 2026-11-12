import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { isEmptyResult } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { IngestApi } from '@files/ingest/ingest-api';
import { attemptReasonKey } from '@files/ingest/ingest-attempt.utils';
import { IngestHistoryStore } from '@files/ingest/history/ingest-history.store';
import { ingestLabelKey, ingestSeverity } from '@files/list/file-grid/file-grid.utils';

@Component({
  imports: [ActionDialog, ButtonDirective, EmptyState, LoadingSpinner, LocalDateTimePipe, Tag, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  providers: [IngestApi, IngestHistoryStore],
  selector: 'hf-ingest-history-host',
  templateUrl: './ingest-history-host.html',
})
export class IngestHistoryHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(IngestHistoryStore);
  protected readonly showSpinner = delayedLoading(this.store.attempts.isLoading);
  protected readonly showEmpty = computed(() =>
    isEmptyResult({
      rows: this.store.rows(),
      isLoading: this.store.attempts.isLoading(),
      hasLoaded: this.store.attempts.hasValue(),
    }),
  );
  protected readonly ingestLabelKey = ingestLabelKey;
  protected readonly ingestSeverity = ingestSeverity;
  protected readonly attemptReasonKey = attemptReasonKey;

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
