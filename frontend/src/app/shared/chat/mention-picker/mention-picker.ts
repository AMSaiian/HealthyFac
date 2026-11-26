import { afterRenderEffect, Component, computed, input, output, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Listbox } from 'primeng/listbox';
import { Popover } from 'primeng/popover';
import { Skeleton } from 'primeng/skeleton';

import { ChatLinkKind } from '@shared/chat/chat';
import type { MentionGroup, MentionOption } from '@shared/chat/mention-picker/mention-picker.utils';
import { delayedLoading } from '@shared/loading/delayed-loading';

const KIND_LABEL_KEYS: Readonly<Record<ChatLinkKind, string>> = {
  [ChatLinkKind.Machine]: 'CHAT.MENTION.KINDS.MACHINE',
  [ChatLinkKind.MaintenanceCase]: 'CHAT.MENTION.KINDS.MAINTENANCE_CASE',
  [ChatLinkKind.File]: 'CHAT.MENTION.KINDS.FILE',
  [ChatLinkKind.Employee]: 'CHAT.MENTION.KINDS.EMPLOYEE',
  [ChatLinkKind.WorkOrder]: 'CHAT.MENTION.KINDS.WORK_ORDER',
};

/** The popover of the `@` mention: the found entities, grouped by kind. It reads no store. */
@Component({
  imports: [Listbox, Popover, Skeleton, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-mention-picker',
  templateUrl: './mention-picker.html',
})
export class MentionPicker {
  readonly groups = input.required<readonly MentionGroup[]>();
  readonly isLoading = input.required<boolean>();
  readonly picked = output<MentionOption>();

  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly isEmpty = computed(() => this.groups().every((group) => group.options.length === 0));
  protected readonly listGroups = computed(() =>
    this.groups()
      .filter((group) => group.options.length > 0)
      .map((group) => ({ labelKey: KIND_LABEL_KEYS[group.kind], items: [...group.options] })),
  );

  private readonly _popover = viewChild.required(Popover);

  constructor() {
    afterRenderEffect(() => {
      void this.listGroups();
      void this.showSkeleton();
      const popover = this._popover();
      if (popover.overlayVisible()) {
        popover.align();
      }
    });
  }

  /** Opens the popover under the target. */
  show(event: Event, target: HTMLElement): void {
    this._popover().show(event, target);
  }

  hide(): void {
    this._popover().hide();
  }

  protected onPick(option: MentionOption | null): void {
    if (option !== null) {
      this.picked.emit(option);
    }
  }
}
