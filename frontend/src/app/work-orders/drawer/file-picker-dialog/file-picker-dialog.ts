import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tooltip } from 'primeng/tooltip';

import type { FilesScope } from '@files/file';
import { HIDDEN_CLOSE_BUTTON_PT } from '@shared/theme/square-button-props';
import { FilePickerGridHost } from '@work-orders/drawer/file-picker-dialog/file-picker-grid-host/file-picker-grid-host';
const COMPANY_TAB = 'company';
const MACHINE_TAB = 'machine';

@Component({
  imports: [
    ButtonDirective,
    Dialog,
    FilePickerGridHost,
    Tab,
    TabList,
    TabPanel,
    TabPanels,
    Tabs,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'contents' },
  selector: 'hf-file-picker-dialog',
  templateUrl: './file-picker-dialog.html',
})
export class FilePickerDialog {
  readonly machineId = input<string | null>(null);
  readonly attachedIds = input.required<readonly string[]>();

  readonly picked = output<readonly string[]>();
  readonly closed = output<void>();

  protected readonly COMPANY_TAB = COMPANY_TAB;
  protected readonly MACHINE_TAB = MACHINE_TAB;
  protected readonly companyScope: FilesScope = { kind: 'company' };
  protected readonly machineScope = computed((): FilesScope | null => {
    const machineId = this.machineId();
    return machineId === null ? null : { kind: 'machine', machineId };
  });
  protected readonly isVisible = signal(true);
  protected readonly hiddenClosePt = HIDDEN_CLOSE_BUTTON_PT;
  protected readonly tab = signal(COMPANY_TAB);
  protected readonly selected = signal<readonly string[]>([]);

  /**
   * A focused tab of PrimeNG `p-tabs` stops every keydown from reaching the document, so PrimeNG's Escape listener
   * misses it. This capture listener on the window closes the picker on Escape and stops the event from reaching the
   * dialog below.
   */
  constructor() {
    const view = inject(DOCUMENT).defaultView;
    const closeOnEscape = (event: KeyboardEvent): void => {
      if (event.key === 'Escape' && this.isVisible()) {
        event.stopPropagation();
        this.isVisible.set(false);
      }
    };
    view?.addEventListener('keydown', closeOnEscape, true);
    inject(DestroyRef).onDestroy(() => view?.removeEventListener('keydown', closeOnEscape, true));
  }

  protected toggle(toggle: { id: string; isSelected: boolean }): void {
    this.selected.update((ids) =>
      toggle.isSelected ? [...ids.filter((id) => id !== toggle.id), toggle.id] : ids.filter((id) => id !== toggle.id),
    );
  }

  protected attach(): void {
    this.picked.emit(this.selected());
  }
}
