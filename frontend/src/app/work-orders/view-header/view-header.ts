import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

export type WorkOrdersView = 'grid' | 'board';

/** The header that the grid page and the board page share: the title, the view links, and Add new. */
@Component({
  imports: [ButtonDirective, RouterLink, TranslatePipe],
  host: { class: 'block flex-none' },
  selector: 'hf-work-orders-view-header',
  templateUrl: './view-header.html',
})
export class ViewHeader {
  readonly activeView = input.required<WorkOrdersView>();
  readonly canCreate = input(false);

  readonly createRequested = output<void>();
}
