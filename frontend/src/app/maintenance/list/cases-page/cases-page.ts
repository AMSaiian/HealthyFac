import { Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { CasesPage as CasesResult } from '@maintenance/case';
import { CasesGrid } from '@maintenance/list/cases-grid/cases-grid';
import { CasesGridStore } from '@maintenance/list/cases-grid.store';

const EMPTY_PAGE: CasesResult = { items: [], totalCount: 0 };

@Component({
  imports: [CasesGrid, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-cases-page',
  templateUrl: './cases-page.html',
})
export class CasesPage {
  protected readonly store = inject(CasesGridStore);

  protected readonly page = computed(() => (this.store.cases.hasValue() ? this.store.cases.value() : EMPTY_PAGE));
}
