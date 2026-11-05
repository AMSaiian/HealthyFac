import { Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Tooltip } from 'primeng/tooltip';

@Component({
  imports: [ButtonDirective, Dialog, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-action-dialog',
  templateUrl: './action-dialog.html',
})
export class ActionDialog {
  readonly title = input.required<string>();
  readonly isSaveDisabled = input(false);
  readonly saveDisabledReason = input<string | null>(null);
  readonly isSaving = input(false);
  readonly save = output<void>();
  readonly cancelled = output<void>();

  protected readonly isVisible = signal(true);
}
