import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';

import type { MachineDraftsPage } from '@machines/drafts/draft';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { DraftGrid } from '@machines/drafts/list/draft-grid/draft-grid';
import { MachineDraftsStore } from '@machines/drafts/list/machine-drafts.store';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

const EMPTY_PAGE: MachineDraftsPage = { items: [], totalCount: 0 };

@Component({
  imports: [ActionDialog, ButtonDirective, DraftGrid, TranslatePipe],
  host: { class: 'contents' },
  providers: [DraftsApi, MetricDefinitionsApi, MetricUnitsApi, StatusDefinitionsApi, MachineDraftsStore],
  selector: 'hf-machine-drafts-host',
  templateUrl: './machine-drafts-host.html',
})
export class MachineDraftsHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(MachineDraftsStore);
  protected readonly page = computed(() => (this.store.drafts.hasValue() ? this.store.drafts.value() : EMPTY_PAGE));

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
