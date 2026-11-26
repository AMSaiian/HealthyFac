import { Component, effect, inject, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Drawer } from 'primeng/drawer';
import { Tooltip } from 'primeng/tooltip';

import { ChatsListStore } from '@assistant/chats-list.store';
import { ChatPicker } from '@shared/chat/chat-picker/chat-picker';
import { ChatView } from '@shared/chat/chat-view/chat-view';
import { CHAT_MESSAGES_PAGE_SIZE } from '@shared/chat/chat.store.utils';
import { ChatStore } from '@shared/chat/chat.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Permission } from '@shared/security/permissions';

/** The compact chat of the shell: a chat picker, New chat, the chat view, and a jump to the chat page. */
@Component({
  imports: [ButtonDirective, ChatPicker, ChatView, Drawer, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  providers: [ChatsListStore],
  selector: 'hf-chat-drawer',
  templateUrl: './chat-drawer.html',
})
export class ChatDrawer {
  protected readonly chat = inject(ChatStore);
  protected readonly list = inject(ChatsListStore);
  protected readonly canWrite = inject(AuthStore).hasPermission(Permission.ChatsWrite);
  protected readonly messagesPageSize = CHAT_MESSAGES_PAGE_SIZE;
  /** The drawer sits under the toolbar, so the toolbar buttons stay reachable. */
  protected readonly drawerStyle = { top: '3.75rem', bottom: '0', height: 'auto' };

  private readonly _router = inject(Router);
  private _seenEndedCount = this.chat.endedCount();
  private _seenDeletedChatId = this.chat.deletedChatId();

  constructor() {
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

  protected openPage(): void {
    const chatId = this.chat.openChatId();
    this._router.navigate(chatId === null ? ['/assistant'] : ['/assistant', chatId]).catch(() => undefined);
  }
}
