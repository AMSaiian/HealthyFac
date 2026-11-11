import { Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Tooltip } from 'primeng/tooltip';

import { SQUARE_CLOSE_BUTTON_PROPS } from '@shared/theme/square-button-props';

@Component({
  imports: [ButtonDirective, Dialog, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-action-dialog',
  templateUrl: './action-dialog.html',
})
export class ActionDialog {
  protected readonly squareCloseButtonProps = SQUARE_CLOSE_BUTTON_PROPS;
  readonly title = input.required<string>();
  readonly isSaveDisabled = input(false);
  readonly saveDisabledReason = input<string | null>(null);
  readonly saveDisabledReasonParams = input<Record<string, unknown> | undefined>(undefined);
  readonly isSaving = input(false);
  /** A read-only dialog hides the save button and shows a close button. */
  readonly isSaveShown = input(true);
  readonly widthClass = input('w-[40rem] max-w-[95vw]');
  /** Hides the footer close button and turns off Escape, so only the cross in the header closes the dialog. */
  readonly isClosedByHeaderOnly = input(false);
  readonly save = output<void>();
  readonly cancelled = output<void>();

  protected readonly isVisible = signal(true);
}
