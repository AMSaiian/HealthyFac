import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { EMPTY_CELL } from '@shared/grid/table/grid-display';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';
import { WorkOrderFiles } from '@work-orders/drawer/work-order-files/work-order-files';
import type { FileEntry } from '@work-orders/drawer/work-order-drawer-store/work-order-drawer.store.utils';
import { CATEGORY_LABEL_KEYS, PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';
import { ESTIMATE_SUFFIX } from '@work-orders/work-order-limits';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';
import { Divider } from 'primeng/divider';

/** The read-only view of a work order: the same fields as `WorkOrderForm`, as static text. */
@Component({
  imports: [Divider, RichTextView, TranslatePipe, WorkOrderFiles],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-work-order-details',
  templateUrl: './work-order-details.html',
})
export class WorkOrderDetails {
  readonly value = input.required<WorkOrderFormValue>();
  readonly machineName = input<string | null>(null);
  readonly assigneeName = input<string | null>(null);
  readonly fileEntries = input.required<readonly FileEntry[]>();
  readonly isFilesLoading = input(false);
  readonly descriptionLinks = input<ReadonlyMap<string, string>>(new Map());

  protected readonly emptyCell = EMPTY_CELL;
  protected readonly startText = computed(
    () => this.value().start?.toString({ smallestUnit: 'minute' }).replace('T', ' ') ?? null,
  );
  protected readonly estimateSuffix = ESTIMATE_SUFFIX;
  protected readonly priorityLabelKeys = PRIORITY_LABEL_KEYS;
  protected readonly categoryLabelKeys = CATEGORY_LABEL_KEYS;
}
