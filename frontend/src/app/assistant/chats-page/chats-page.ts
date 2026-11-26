import { Component, effect, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { ChatList } from '@assistant/chat-list/chat-list';
import { CHATS_PAGE_SIZE } from '@assistant/chats-list.store.utils';
import { ChatsListStore } from '@assistant/chats-list.store';
import { ChatStore } from '@shared/chat/chat.store';
import { EmptyState } from '@shared/loading/empty-state/empty-state';

@Component({
  imports: [ChatList, EmptyState, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 gap-4 p-4' },
  selector: 'hf-chats-page',
  templateUrl: './chats-page.html',
})
export class ChatsPage {
  /** The chat of the route. The router binds the param `chatId`. */
  readonly chatId = input<string>();

  protected readonly list = inject(ChatsListStore);
  protected readonly chat = inject(ChatStore);
  protected readonly pageSize = CHATS_PAGE_SIZE;

  constructor() {
    effect(() => {
      this.chat.openChat(this.chatId() ?? null);
    });
  }
}
