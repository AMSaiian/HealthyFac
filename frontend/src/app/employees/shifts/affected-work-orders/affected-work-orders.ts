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
  /** The hint key. The edit dialog tells the user to save again; the delete dialog tells the user to delete again. */
  readonly hintKey = input('SHIFTS.AFFECTED.HINT');
  /** The heading key. The delete dialog passes `null`, because its title already names the problem. */
  readonly titleKey = input<string | null>('SHIFTS.AFFECTED.TITLE');

  readonly opened = output<string>();
}
