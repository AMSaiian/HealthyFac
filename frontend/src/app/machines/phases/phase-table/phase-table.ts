import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { TableModule, type TableRowReorderEvent } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import type { OperatingPhase } from '@machines/phases/operating-phase';
import { CellText } from '@shared/grid/table/cell-text/cell-text';
import { TEXT_COLUMN_WIDTH } from '@shared/grid/table/grid-table';

@Component({
  imports: [CellText, ButtonDirective, TableModule, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-phase-table',
  templateUrl: './phase-table.html',
})
export class PhaseTable {
  readonly rows = input.required<readonly OperatingPhase[]>();
  readonly canWrite = input(false);

  protected readonly textColumnWidth = TEXT_COLUMN_WIDTH;

  readonly reordered = output<{ from: number; to: number }>();
  readonly updateRequested = output<string>();
  readonly deleteRequested = output<OperatingPhase>();

  protected onRowReorder(event: TableRowReorderEvent): void {
    this.reordered.emit({ from: event.dragIndex ?? 0, to: event.dropIndex ?? 0 });
  }
}
