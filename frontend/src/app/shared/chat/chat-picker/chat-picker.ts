import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Select, type SelectLazyLoadEvent } from 'primeng/select';

import type { ChatListItem } from '@shared/chat/chat';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';

/** The height of one option in pixels. The virtual scroll needs a fixed size. */
export const CHAT_OPTION_HEIGHT = 36;

/** A lazy select of the chats of the user. It asks for the next page near the end of the loaded options. */
@Component({
  imports: [EmptyState, FormsModule, Select, TranslatePipe],
  host: { class: 'flex min-w-0 flex-1' },
  selector: 'hf-chat-picker',
  templateUrl: './chat-picker.html',
})
export class ChatPicker {
  readonly items = input.required<readonly ChatListItem[]>();
  readonly selectedId = input<string | null>(null);
  readonly hasMore = input.required<boolean>();
  readonly isLoading = input.required<boolean>();
  readonly hasLoaded = input.required<boolean>();
  readonly chatSelected = output<string>();
  readonly nextPageRequested = output();

  protected readonly optionHeight = CHAT_OPTION_HEIGHT;
  protected readonly options = computed(() => [...this.items()]);
  protected readonly showLoading = delayedLoading(this.isLoading);

  protected onLazyLoad(event: SelectLazyLoadEvent): void {
    if (this.hasMore() && !this.isLoading() && event.last >= this.items().length - 1) {
      this.nextPageRequested.emit();
    }
  }

  protected onChange(chatId: string | null): void {
    if (chatId !== null) {
      this.chatSelected.emit(chatId);
    }
  }
}
