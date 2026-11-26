import {
  afterRenderEffect,
  Component,
  computed,
  type ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Avatar } from 'primeng/avatar';
import { ButtonDirective } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { type ChatActivity, type ChatMessage, ChatMessageRole, ChatMessageState } from '@shared/chat/chat';
import { activityKey, isClamped, messageNoteKey } from '@shared/chat/chat-message/chat-message.utils';
import { delayedLoading } from '@shared/loading/delayed-loading';

/** The path of the avatar of the assistant. It is the app icon. */
export const ASSISTANT_AVATAR = 'favicon.svg';

/** The path of the one avatar of every user. */
export const USER_AVATAR = 'images/avatars/user.svg';

/** One message of a chat: the assistant on the left, the user on the right. It reads no store. */
@Component({
  imports: [Avatar, ButtonDirective, Skeleton, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex min-w-0 flex-none' },
  selector: 'hf-chat-message',
  templateUrl: './chat-message.html',
})
export class ChatMessageView {
  readonly message = input.required<ChatMessage>();
  /** The text that grows while the turn streams. It replaces the stored text of a streaming message. */
  readonly streamingText = input<string | null>(null);
  readonly activity = input<ChatActivity | null>(null);
  readonly canDelete = input(false);
  readonly deleteRequested = output<ChatMessage>();

  protected readonly isExpanded = signal(false);
  /** Tells whether the clamped text hides lines. It follows the rendered text, so a narrow box also counts. */
  protected readonly isOverflowing = signal(false);
  protected readonly assistantAvatar = ASSISTANT_AVATAR;
  protected readonly userAvatar = USER_AVATAR;

  protected readonly isUser = computed(() => this.message().role === ChatMessageRole.User);
  protected readonly isStreaming = computed(() => this.message().state === ChatMessageState.Streaming);
  protected readonly text = computed(() => this.streamingText() ?? this.message().text);
  protected readonly noteKey = computed(() => messageNoteKey(this.message()));
  protected readonly activityKey = computed(() => activityKey(this.activity()));
  protected readonly isWaiting = computed(() => this.isStreaming() && this.text() === '');
  protected readonly showSkeleton = delayedLoading(this.isWaiting);

  private readonly _paragraph = viewChild<ElementRef<HTMLElement>>('paragraph');

  constructor() {
    afterRenderEffect((onCleanup) => {
      void this.text();
      const element = this._paragraph()?.nativeElement;
      if (element === undefined) {
        this.isOverflowing.set(false);
        return;
      }
      const measure = (): void => {
        if (!this.isExpanded()) {
          this.isOverflowing.set(isClamped(element.scrollHeight, element.clientHeight));
        }
      };
      measure();
      const observer = new ResizeObserver(measure);
      observer.observe(element);
      onCleanup(() => {
        observer.disconnect();
      });
    });
  }

  protected toggleExpanded(): void {
    this.isExpanded.update((isExpanded) => !isExpanded);
  }
}
