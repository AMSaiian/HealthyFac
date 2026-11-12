import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, inject, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Tooltip } from 'primeng/tooltip';

import { hasOpenPanel, isDialogTopmost } from '@shared/actions/action-dialog/action-dialog.utils';
import { HIDDEN_CLOSE_BUTTON_PT } from '@shared/theme/square-button-props';

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
  readonly saveDisabledReasonParams = input<Record<string, unknown> | undefined>(undefined);
  readonly isSaving = input(false);
  /** A read-only dialog hides the save button and shows a close button. */
  readonly isSaveShown = input(true);
  readonly widthClass = input('w-[40rem] max-w-[95vw]');
  /** A grid body fills the dialog height and does not scroll: the grid scrolls inside it. */
  readonly hasGrid = input(false);
  readonly save = output<void>();
  readonly cancelled = output<void>();

  protected readonly isVisible = signal(true);
  private readonly _dialog = viewChild.required(Dialog);
  protected readonly hiddenClosePt = HIDDEN_CLOSE_BUTTON_PT;
  protected readonly rootClass = computed(() =>
    this.hasGrid() ? `${this.widthClass()} h-[85vh] flex flex-col` : this.widthClass(),
  );
  protected readonly bodyClass = computed(() =>
    this.hasGrid() ? 'relative flex min-h-0 flex-1 flex-col p-1' : 'relative p-1',
  );
  protected readonly contentStyle = computed((): Record<string, string> =>
    this.hasGrid()
      ? { display: 'flex', 'flex-direction': 'column', 'min-height': '0', overflow: 'hidden' }
      : { 'max-height': '70vh', 'overflow-y': 'auto' },
  );

  /**
   * Escape closes the dialog here, not in PrimeNG: PrimeNG closes a dialog on Escape only while its z-index is the
   * newest one that any overlay took, and a closed overlay of a filter can leave a newer z-index, so Escape did nothing
   * after a choice in a filter select. This listener closes the dialog when no overlay panel is open, no other dialog
   * stands above it, and no save runs.
   */
  constructor() {
    const doc = inject(DOCUMENT);
    const view = doc.defaultView;
    const closeOnEscape = (event: KeyboardEvent): void => {
      const container = this._dialog().container();
      if (
        event.key === 'Escape' &&
        !this.isSaving() &&
        container instanceof HTMLElement &&
        isDialogTopmost(container, doc) &&
        !hasOpenPanel(doc)
      ) {
        this.isVisible.set(false);
      }
    };
    view?.addEventListener('keydown', closeOnEscape);
    inject(DestroyRef).onDestroy(() => view?.removeEventListener('keydown', closeOnEscape));
  }
}
