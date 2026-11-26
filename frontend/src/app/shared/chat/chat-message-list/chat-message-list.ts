import type { ElementRef } from '@angular/core';
import { afterRenderEffect, Component, computed, DestroyRef, inject, input, output, viewChild } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

import type { ChatDraft, ChatMessage } from '@shared/chat/chat';
import { ChatMessageView } from '@shared/chat/chat-message/chat-message';
import {
  anchoredScrollTop,
  deletableMessageIds,
  isScrollKey,
  isUserScroll,
  newestUserMessageId,
  pinAfterUserInput,
  pinsOnChange,
  type PinWatch,
  shouldFollowGrowth,
  shouldLoadOlder,
} from '@shared/chat/chat-message-list/chat-message-list.utils';
import type { RunningTurn } from '@shared/chat/chat.store.utils';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { isEmptyResult, skeletonRows } from '@shared/grid/table/grid-display';

/** How long after a user input a scroll event counts as the scroll of the user. */
const USER_SCROLL_WINDOW_MILLISECONDS = 400;

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
  readonly chatId = input<string | null>(null);

  readonly olderRequested = output();
  readonly deleteRequested = output<ChatMessage>();
  readonly draftJumpRequested = output<ChatDraft>();

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
  private _userScrollUntil = 0;
  private _isPointerHeld = false;
  private _pinWatch: PinWatch = { newestUserId: null, isRunning: false };
  private _lastChatId: string | null = null;
  private readonly _growthObserver = new ResizeObserver(() => {
    const element = this._scroller()?.nativeElement;
    if (element !== undefined && shouldFollowGrowth(this._isPinned, this._heightBeforeOlder !== null)) {
      this._scrollToBottom(element);
    }
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this._growthObserver.disconnect();
    });
    afterRenderEffect(() => {
      void this.ordered();
      void this.running()?.text;
      void this.isLoadingOlder();
      const chatId = this.chatId();
      if (chatId !== this._lastChatId) {
        this._lastChatId = chatId;
        this._isPinned = true;
        this._pinWatch = { newestUserId: null, isRunning: false };
      }
      const watch: PinWatch = {
        newestUserId: newestUserMessageId(this.messages()),
        isRunning: this.running() !== null,
      };
      if (pinsOnChange(this._pinWatch, watch)) {
        this._isPinned = true;
      }
      this._pinWatch = watch;
      const element = this._scroller()?.nativeElement;
      if (element === undefined) {
        return;
      }
      for (const child of Array.from(element.children)) {
        this._growthObserver.observe(child);
      }
      if (this._heightBeforeOlder !== null && !this.isLoadingOlder()) {
        element.scrollTop = anchoredScrollTop(element.scrollTop, this._heightBeforeOlder, element.scrollHeight);
        this._heightBeforeOlder = null;
      } else if (this._isPinned) {
        this._scrollToBottom(element);
      }
    });
  }

  protected onUserInput(element: HTMLElement, key?: string): void {
    if (key !== undefined && !isScrollKey(key)) {
      return;
    }
    this._userScrollUntil = performance.now() + USER_SCROLL_WINDOW_MILLISECONDS;
    this._isPinned = pinAfterUserInput(element);
  }

  protected onPointerDown(element: HTMLElement): void {
    this.onUserInput(element);
    this._isPointerHeld = true;
    const release = (): void => {
      this._isPointerHeld = false;
      window.removeEventListener('pointerup', release);
      window.removeEventListener('pointercancel', release);
    };
    window.addEventListener('pointerup', release);
    window.addEventListener('pointercancel', release);
  }

  protected onScroll(element: HTMLElement): void {
    if (isUserScroll(performance.now(), this._userScrollUntil, this._isPointerHeld)) {
      this._isPinned = pinAfterUserInput(element);
    }
    if (shouldLoadOlder(element.scrollTop) && this.hasMore() && !this.isLoadingOlder() && !this.isLoading()) {
      this._heightBeforeOlder = element.scrollHeight;
      this.olderRequested.emit();
    }
  }

  private _scrollToBottom(element: HTMLElement): void {
    element.scrollTop = element.scrollHeight;
  }
}
