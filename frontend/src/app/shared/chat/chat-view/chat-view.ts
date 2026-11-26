import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Message } from 'primeng/message';
import { Tooltip } from 'primeng/tooltip';

import { type Chat, type ChatMessage, ChatState } from '@shared/chat/chat';
import { ChatComposer } from '@shared/chat/chat-composer/chat-composer';
import { ChatMessageList } from '@shared/chat/chat-message-list/chat-message-list';
import type { RunningTurn } from '@shared/chat/chat.store.utils';

/** One chat: the title row, the messages, the blocked banner or the composer. It reads no store. */
@Component({
  imports: [ButtonDirective, ChatComposer, ChatMessageList, Message, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-2' },
  selector: 'hf-chat-view',
  templateUrl: './chat-view.html',
})
export class ChatView {
  readonly chat = input<Chat | null>(null);
  readonly messages = input.required<readonly ChatMessage[]>();
  readonly running = input<RunningTurn | null>(null);
  readonly isLoading = input.required<boolean>();
  readonly isLoadingOlder = input.required<boolean>();
  readonly hasMore = input.required<boolean>();
  readonly limit = input.required<number>();
  readonly isRunning = input(false);
  readonly isStopping = input(false);
  readonly canStop = input(false);
  readonly unsentText = input<string | null>(null);
  readonly hasLoaded = input.required<boolean>();
  readonly canWrite = input(false);

  readonly olderRequested = output();
  readonly sendRequested = output<string>();
  readonly stopRequested = output();
  readonly deleteMessageRequested = output<ChatMessage>();
  readonly deleteChatRequested = output();

  protected readonly isBlocked = computed(() => this.chat()?.state === ChatState.Blocked);
}
