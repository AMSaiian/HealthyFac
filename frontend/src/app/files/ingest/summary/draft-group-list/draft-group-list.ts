import { Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import type { DraftGroupView, DraftRow } from '@files/ingest/summary/ingest-summary.store.utils';
import { DraftKind, DraftState } from '@machines/drafts/draft';
import { draftRowMenuItems } from '@machines/drafts/draft-row-menu';

const KIND_KEYS: Readonly<Record<DraftKind, string>> = {
  [DraftKind.Machine]: 'DRAFTS.KINDS.MACHINE',
  [DraftKind.Metric]: 'DRAFTS.KINDS.METRIC',
  [DraftKind.Status]: 'DRAFTS.KINDS.STATUS',
};

const STATE_KEYS: Readonly<Record<DraftState, string>> = {
  [DraftState.Pending]: 'DRAFTS.STATES.PENDING',
  [DraftState.Applied]: 'DRAFTS.STATES.APPLIED',
  [DraftState.Dismissed]: 'DRAFTS.STATES.DISMISSED',
};

const STATE_SEVERITIES: Readonly<Record<DraftState, 'info' | 'success' | 'secondary'>> = {
  [DraftState.Pending]: 'info',
  [DraftState.Applied]: 'success',
  [DraftState.Dismissed]: 'secondary',
};

@Component({
  imports: [ButtonDirective, Menu, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-draft-group-list',
  templateUrl: './draft-group-list.html',
})
export class DraftGroupList {
  readonly groups = input.required<readonly DraftGroupView[]>();
  readonly canWrite = input(false);
  /** The reason that **Apply** is off for each draft ID. A draft without a reason applies as it is. */
  readonly reasons = input<Readonly<Record<string, string | null>>>({});
  /** The reason that **Apply all** is off for each group key. */
  readonly groupReasons = input<Readonly<Record<string, string | null>>>({});
  readonly applyingIds = input<readonly string[]>([]);

  readonly applyRequested = output<DraftRow>();
  readonly applyAllRequested = output<string>();
  readonly reviewRequested = output<DraftRow>();
  readonly dismissRequested = output<DraftRow>();

  protected readonly isBusy = computed(() => this.applyingIds().length > 0);
  protected readonly menuItems = signal<MenuItem[]>([]);
  private readonly _translate = inject(TranslateService);

  protected readonly kindKey = (kind: DraftKind): string => KIND_KEYS[kind];
  protected readonly stateKey = (state: DraftState): string => STATE_KEYS[state];
  protected readonly stateSeverity = (state: DraftState): 'info' | 'success' | 'secondary' => STATE_SEVERITIES[state];

  /** Builds the menu of the row at the click, so the texts follow the current language. */
  protected openMenu(event: Event, row: DraftRow, menu: Menu): void {
    this.menuItems.set(
      draftRowMenuItems({
        translate: (key) => this._translate.instant(key),
        isReviewDisabled: row.isReviewDisabled,
        applyReasonKey: this.reasons()[row.id],
        isBusy: this.isBusy(),
        onReview: () => this.reviewRequested.emit(row),
        onApply: () => this.applyRequested.emit(row),
        onDismiss: () => this.dismissRequested.emit(row),
      }),
    );
    menu.toggle(event);
  }
}
