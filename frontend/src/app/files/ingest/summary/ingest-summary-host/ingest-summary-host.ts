import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import type { AppAction } from '@shared/actions/app-action';

import { IngestApi } from '@files/ingest/ingest-api';
import { toKeySegment } from '@files/ingest/ingest-attempt.utils';
import { IngestSummaryStore } from '@files/ingest/summary/ingest-summary.store';
import { MachineDraftsHost } from '@machines/drafts/list/machine-drafts-host/machine-drafts-host';

/** The ingest summary is the shared drafts dialog of one attempt, with the notices of the attempt above the list. */
@Component({
  imports: [MachineDraftsHost, TranslatePipe],
  host: { class: 'contents' },
  providers: [IngestApi, IngestSummaryStore],
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
