import {
  afterNextRender,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Scroller, type ScrollerLazyLoadEvent } from 'primeng/scroller';
import { Skeleton } from 'primeng/skeleton';

import { ASSISTANT_PATH } from '@assistant/assistant-paths';
import { listSlots, needsNextPage } from '@assistant/chat-list/chat-list.utils';
import type { ChatListItem } from '@shared/chat/chat';
import { isEmptyResult } from '@shared/grid/table/grid-display';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

/** The height of one row of the list in pixels. The lazy scroller needs a fixed size. */
export const CHAT_ROW_HEIGHT = 64;

@Component({
  imports: [EmptyState, LocalDateTimePipe, RouterLink, Scroller, Skeleton, TranslatePipe],
  host: { class: 'flex h-full min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-chat-list',
  templateUrl: './chat-list.html',
})
export class ChatList {
  readonly items = input.required<readonly ChatListItem[]>();
  readonly hasMore = input.required<boolean>();
  readonly isLoading = input.required<boolean>();
  readonly isLoadingNext = input.required<boolean>();
  readonly hasLoaded = input.required<boolean>();
  readonly limit = input.required<number>();
  readonly selectedId = input<string | null>(null);
  readonly nextPageRequested = output();

  protected readonly rowHeight = CHAT_ROW_HEIGHT;
  protected readonly assistantPath = ASSISTANT_PATH;
  protected readonly showSkeleton = delayedLoading(computed(() => this.isLoading() || this.isLoadingNext()));
  protected readonly slots = computed(() => listSlots(this.items(), this.hasMore(), this.isLoading(), this.limit()));
  protected readonly showEmpty = computed(() =>
    isEmptyResult({ rows: this.items(), isLoading: this.isLoading(), hasLoaded: this.hasLoaded() }),
  );

  private readonly _scroller = viewChild(Scroller);
  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _destroyRef = inject(DestroyRef);

  constructor() {
    effect(() => {
      void this.slots();
      this._scroller()?.init();
    });
    afterNextRender(() => {
      const observer = new ResizeObserver(() => this._scroller()?.init());
      observer.observe(this._host.nativeElement);
      this._destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  protected onLazyLoad(event: ScrollerLazyLoadEvent): void {
    if (this.hasMore() && needsNextPage(event.last, this.items().length)) {
      this.nextPageRequested.emit();
    }
  }
}
