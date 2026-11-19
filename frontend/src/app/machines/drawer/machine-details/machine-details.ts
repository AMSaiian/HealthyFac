import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { EMPTY_CELL } from '@shared/grid/table/grid-display';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';
import { PickerDatePipe } from '@shared/time/picker-date.pipe';
import { STATE_LABEL_KEYS } from '@machines/list/machine-grid/machine-grid.utils';
import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

import { copyText } from '@shared/clipboard/copy-text';
import { Divider } from 'primeng/divider';

/** The read-only view of a machine: the same fields as `MachineForm`, as static text. */
@Component({
  imports: [Divider, ButtonDirective, PickerDatePipe, RichTextView, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-machine-details',
  templateUrl: './machine-details.html',
})
export class MachineDetails {
  readonly value = input.required<MachineFormValue>();
  readonly gatewayId = input<string | null>(null);
  readonly descriptionLinks = input<ReadonlyMap<string, string>>(new Map());

  protected readonly emptyCell = EMPTY_CELL;
  protected readonly stateLabelKeys = STATE_LABEL_KEYS;

  protected copyGatewayId(): void {
    const gatewayId = this.gatewayId();
    if (gatewayId !== null) {
      copyText(gatewayId);
    }
  }
}
