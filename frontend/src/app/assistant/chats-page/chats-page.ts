import { Component, effect, inject, input, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { ChatList } from '@assistant/chat-list/chat-list';
import { CHATS_PAGE_SIZE } from '@assistant/chats-list.store.utils';
import { ChatsListStore } from '@assistant/chats-list.store';
import { ChatView } from '@shared/chat/chat-view/chat-view';
import { CHAT_MESSAGES_PAGE_SIZE } from '@shared/chat/chat.store.utils';
import { ChatStore } from '@shared/chat/chat.store';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

@Component({
  imports: [ButtonDirective, ChatList, ChatView, EmptyState, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-chats-page',
  templateUrl: './chats-page.html',
})
export class ChatsPage {
  /** The chat of the route. The router binds the param `chatId`. */
  readonly chatId = input<string>();

  protected readonly list = inject(ChatsListStore);
  protected readonly chat = inject(ChatStore);
  protected readonly pageSize = CHATS_PAGE_SIZE;
  protected readonly messagesPageSize = CHAT_MESSAGES_PAGE_SIZE;
  protected readonly canWrite = inject(AuthStore).hasPermission(Permission.ChatsWrite);

  private readonly _router = inject(Router);
  private _seenEndedCount = this.chat.endedCount();
  private _seenDeletedChatId = this.chat.deletedChatId();

  constructor() {
    effect(() => {
      const chatId = this.chatId() ?? null;
      untracked(() => {
        this.chat.openChat(chatId);
      });
    });
    effect(() => {
      const createdChatId = this.chat.createdChatId();
      if (createdChatId !== null && createdChatId !== this.chatId()) {
        untracked(() => {
          void this._router.navigate(['/assistant', createdChatId]);
        });
      }
    });
    effect(() => {
      if (this.chat.deletedChatId() !== null) {
        untracked(() => {
          void this._router.navigate(['/assistant']);
        });
      }
    });
    effect(() => {
      const endedCount = this.chat.endedCount();
      const deletedChatId = this.chat.deletedChatId();
      const isTurnEnded = endedCount !== this._seenEndedCount;
      const isChatDeleted = deletedChatId !== null && deletedChatId !== this._seenDeletedChatId;
      this._seenEndedCount = endedCount;
      this._seenDeletedChatId = deletedChatId;
      if (!isTurnEnded && !isChatDeleted) {
        return;
      }
      untracked(() => {
        this.list.reload();
      });
    });
  }

  protected openNewChat(): void {
    void this._router.navigate(['/assistant']);
  }
}
