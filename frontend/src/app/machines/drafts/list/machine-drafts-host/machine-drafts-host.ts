import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';
import { delayedLoading } from '@shared/loading/delayed-loading';

import { DraftState, type MachineDraftsPage } from '@machines/drafts/draft';
import { DraftsApi } from '@machines/drafts/drafts-api';
import { DraftGrid } from '@machines/drafts/list/draft-grid/draft-grid';
import { MachineDraftsStore } from '@machines/drafts/list/machine-drafts.store';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricUnitsApi } from '@machines/metrics/metric-units-api';
import { StatusDefinitionsApi } from '@machines/statuses/status-definitions-api';

const EMPTY_PAGE: MachineDraftsPage = { items: [], totalCount: 0 };

@Component({
  imports: [ActionDialog, ButtonDirective, DraftGrid, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  providers: [DraftsApi, MetricDefinitionsApi, MetricUnitsApi, StatusDefinitionsApi, MachineDraftsStore],
  selector: 'hf-machine-drafts-host',
  templateUrl: './machine-drafts-host.html',
})
export class MachineDraftsHost {
  readonly action = input.required<AppAction>();
  /** The title of the dialog: the machine list, or the ingest summary. */
  readonly titleKey = input('DRAFTS.LIST.TITLE');

  protected readonly store = inject(MachineDraftsStore);
  protected readonly showDismissing = delayedLoading(this.store.isDismissing);
  protected readonly showQuickBusy = delayedLoading(this.store.isQuickBusy);
  protected readonly page = computed(() => (this.store.drafts.hasValue() ? this.store.drafts.value() : EMPTY_PAGE));

  /** The pending drafts of the page: the drafts that the header check can check. */
  protected readonly pendingItems = computed(() =>
    this.page().items.filter((item) => item.state === DraftState.Pending),
  );
  /** **Dismiss** and **Apply** work on the checked drafts when there are some, else on every pending draft. */
  protected readonly isActionDisabled = computed(
    () =>
      (this.store.selectedCount() === 0 && this.store.pendingCount() === 0) ||
      this.store.isQuickBusy() ||
      this.store.isDismissing(),
  );

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
