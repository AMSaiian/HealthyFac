import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { isEmptyResult } from '@shared/grid/table/grid-display';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';

import { OperatingPhasesPageStore } from '@machines/phases/operating-phases-page-store/operating-phases-page.store';
import { PhaseDialogHost } from '@machines/phases/phase-dialog-host/phase-dialog-host';
import { PhaseTable } from '@machines/phases/phase-table/phase-table';

@Component({
  imports: [
    ButtonDirective,
    EmptyState,
    LoadingSpinner,
    PhaseDialogHost,
    PhaseTable,
    RouterLink,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-operating-phases-page',
  templateUrl: './operating-phases-page.html',
})
export class OperatingPhasesPage {
  protected readonly store = inject(OperatingPhasesPageStore);

  protected readonly showSpinner = delayedLoading(
    computed(() => this.store.phases.isLoading() && this.store.items().length === 0),
  );
  protected readonly showEmpty = computed(() =>
    isEmptyResult({
      rows: this.store.items(),
      isLoading: this.store.phases.isLoading(),
      hasLoaded: this.store.phases.hasValue(),
    }),
  );
}
