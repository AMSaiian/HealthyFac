import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import type { StatusDefinitionsPage as StatusDefinitionsResult } from '@machines/statuses/status-definition';
import { StatusDefinitionDrawerHost } from '@machines/statuses/drawer/status-definition-drawer-host/status-definition-drawer-host';
import { StatusDefinitionGrid } from '@machines/statuses/list/status-definition-grid/status-definition-grid';
import { StatusDefinitionsGridStore } from '@machines/statuses/list/status-definitions-grid.store';

const EMPTY_PAGE: StatusDefinitionsResult = { items: [], totalCount: 0 };

@Component({
  imports: [ButtonDirective, StatusDefinitionDrawerHost, StatusDefinitionGrid, RouterLink, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-status-definitions-page',
  templateUrl: './status-definitions-page.html',
})
export class StatusDefinitionsPage {
  protected readonly store = inject(StatusDefinitionsGridStore);

  protected readonly page = computed(() => (this.store.statuses.hasValue() ? this.store.statuses.value() : EMPTY_PAGE));
}
