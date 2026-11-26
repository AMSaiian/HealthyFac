import type { ElementRef } from '@angular/core';
import { afterRenderEffect, Component, computed, input, output, viewChild } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

import type { ChatMessage } from '@shared/chat/chat';
import { ChatMessageView } from '@shared/chat/chat-message/chat-message';
import {
  anchoredScrollTop,
  deletableMessageIds,
  isPinnedToBottom,
  shouldLoadOlder,
} from '@shared/chat/chat-message-list/chat-message-list.utils';
import type { RunningTurn } from '@shared/chat/chat.store.utils';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { isEmptyResult, skeletonRows } from '@shared/grid/table/grid-display';

/** The messages of a chat in one scroll container (the only scroll boundary of the chat). It reads no store. */
@Component({
  imports: [ChatMessageView, EmptyState, Skeleton],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-chat-message-list',
  templateUrl: './chat-message-list.html',
})
export class ChatMessageList {
  /** The loaded messages, the newest first. */
  readonly messages = input.required<readonly ChatMessage[]>();
  readonly running = input<RunningTurn | null>(null);
  readonly isLoading = input.required<boolean>();
  readonly isLoadingOlder = input.required<boolean>();
  readonly hasMore = input.required<boolean>();
  readonly hasLoaded = input.required<boolean>();
  readonly limit = input.required<number>();
  readonly canDelete = input(false);

  readonly olderRequested = output();
  readonly deleteRequested = output<ChatMessage>();

  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly showOlderSkeleton = delayedLoading(this.isLoadingOlder);
  protected readonly ordered = computed(() => [...this.messages()].reverse());
  /** The stored user messages whose reply is not streaming. A reply and a pending message have no delete. */
  protected readonly deletableIds = computed(() => deletableMessageIds(this.messages()));
  protected readonly skeletonItems = computed(() => skeletonRows(this.limit()));
  protected readonly showEmpty = computed(() =>
    isEmptyResult({ rows: this.messages(), isLoading: this.isLoading(), hasLoaded: this.hasLoaded() }),
  );

  private readonly _scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private _isPinned = true;
  private _heightBeforeOlder: number | null = null;

  constructor() {
    afterRenderEffect(() => {
      void this.ordered();
      void this.running()?.text;
      void this.isLoadingOlder();
      const element = this._scroller()?.nativeElement;
      if (element === undefined) {
        return;
      }
      if (this._heightBeforeOlder !== null && !this.isLoadingOlder()) {
        element.scrollTop = anchoredScrollTop(element.scrollTop, this._heightBeforeOlder, element.scrollHeight);
        this._heightBeforeOlder = null;
      } else if (this._isPinned) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }

  protected onScroll(element: HTMLElement): void {
    this._isPinned = isPinnedToBottom(element.scrollTop, element.clientHeight, element.scrollHeight);
    if (shouldLoadOlder(element.scrollTop) && this.hasMore() && !this.isLoadingOlder() && !this.isLoading()) {
      this._heightBeforeOlder = element.scrollHeight;
      this.olderRequested.emit();
    }
  }
}
