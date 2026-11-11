import { Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';

import type { FilesScope } from '@files/file';
import { FilePickerGridHost } from '@work-orders/drawer/file-picker-dialog/file-picker-grid-host/file-picker-grid-host';

import { SQUARE_CLOSE_BUTTON_PROPS } from '@shared/theme/square-button-props';

const COMPANY_TAB = 'company';
const MACHINE_TAB = 'machine';

@Component({
  imports: [ButtonDirective, Dialog, FilePickerGridHost, Tab, TabList, TabPanel, TabPanels, Tabs, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-file-picker-dialog',
  templateUrl: './file-picker-dialog.html',
})
export class FilePickerDialog {
  protected readonly squareCloseButtonProps = SQUARE_CLOSE_BUTTON_PROPS;
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
  protected readonly tab = signal(COMPANY_TAB);
  protected readonly selected = signal<readonly string[]>([]);

  protected toggle(toggle: { id: string; isSelected: boolean }): void {
    this.selected.update((ids) =>
      toggle.isSelected ? [...ids.filter((id) => id !== toggle.id), toggle.id] : ids.filter((id) => id !== toggle.id),
    );
  }

  protected attach(): void {
    this.picked.emit(this.selected());
  }
}
