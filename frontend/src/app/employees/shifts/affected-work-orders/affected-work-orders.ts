import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AffectedWorkOrder } from '@employees/shifts/shift';

/** The work orders that a rejected shift change names. Each entry opens the work order. */
@Component({
  imports: [TranslatePipe],
  host: { class: 'hf-list-item mb-4 flex flex-col gap-1' },
  selector: 'hf-affected-work-orders',
  templateUrl: './affected-work-orders.html',
})
export class AffectedWorkOrders {
  readonly workOrders = input.required<readonly AffectedWorkOrder[]>();

  readonly opened = output<string>();
}
