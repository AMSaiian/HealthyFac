import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tooltip } from 'primeng/tooltip';

export type WorkOrdersView = 'grid' | 'board';

/**
 * The header that the grid page and the board page share: the title and Add new, with the view tabs below. The page
 * content sits in the tab panel, so its gap to the tabs matches the employees page.
 */
@Component({
  imports: [ButtonDirective, RouterLink, Tab, TabList, TabPanel, TabPanels, Tabs, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4' },
  selector: 'hf-work-orders-view-header',
  templateUrl: './view-header.html',
})
export class ViewHeader {
  readonly activeView = input.required<WorkOrdersView>();
  readonly canCreate = input(false);

  readonly createRequested = output<void>();
}
