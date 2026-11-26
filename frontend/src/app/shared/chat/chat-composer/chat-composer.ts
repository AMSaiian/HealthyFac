import type { ElementRef } from '@angular/core';
import { Component, computed, effect, input, linkedSignal, output, signal, untracked, viewChild } from '@angular/core';
import { form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import { Tooltip } from 'primeng/tooltip';

import { chatComposerSchema } from '@shared/chat/chat-composer/chat-composer-schema';
import {
  type ChatComposerSend,
  isCountShown,
  sendDisabledReason,
} from '@shared/chat/chat-composer/chat-composer.utils';
import { CHAT_TEXT_MAX_LENGTH } from '@shared/chat/chat-limits';
import { MentionPicker } from '@shared/chat/mention-picker/mention-picker';
import {
  applyMention,
  canAddLink,
  linksForSend,
  type MentionGroup,
  type MentionOption,
  type MentionQuery,
  mentionQueryAt,
} from '@shared/chat/mention-picker/mention-picker.utils';

/** The box for a new message with Send, Stop, and the `@` mentions. It reads no store. */
@Component({
  imports: [ButtonDirective, FormField, MentionPicker, Tag, Textarea, Tooltip, TranslatePipe],
  host: { class: 'flex flex-none flex-col gap-2' },
  selector: 'hf-chat-composer',
  templateUrl: './chat-composer.html',
})
export class ChatComposer {
  readonly isRunning = input(false);
  readonly isStopping = input(false);
  /** Tells whether this tab sends the open turn, so Stop can abort it. */
  readonly canStop = input(false);
  /** The text of a failed send. The box takes it back when it is empty. */
  readonly unsentText = input<string | null>(null);
  readonly mentionGroups = input<readonly MentionGroup[]>([]);
  readonly isMentionLoading = input(false);

  readonly sendRequested = output<ChatComposerSend>();
  readonly stopRequested = output<void>();
  readonly mentionSearchRequested = output<string>();

  protected readonly maxLength = CHAT_TEXT_MAX_LENGTH;
  protected readonly model = linkedSignal(() => ({ text: '' }));
  protected readonly composerForm = form(this.model, (path) => {
    validateStandardSchema(path, chatComposerSchema);
  });
  protected readonly links = signal<readonly MentionOption[]>([]);
  protected readonly disabledReason = computed(() =>
    sendDisabledReason({
      text: this.model().text,
      isRunning: this.isRunning(),
      isStopping: this.isStopping(),
    }),
  );
  protected readonly showCount = computed(() => isCountShown(this.model().text.length));

  private readonly _textarea = viewChild.required<ElementRef<HTMLTextAreaElement>>('textarea');
  private readonly _picker = viewChild.required(MentionPicker);
  private _mention: MentionQuery | null = null;

  constructor() {
    effect(() => {
      const unsent = this.unsentText();
      if (unsent === null) {
        return;
      }
      untracked(() => {
        if (this.model().text === '') {
          this.model.set({ text: unsent });
        }
      });
    });
  }

  protected onEnter(event: Event): void {
    const keyboard = event as KeyboardEvent;
    if (keyboard.shiftKey) {
      return;
    }
    keyboard.preventDefault();
    this.onSend();
  }

  protected onTyping(event: Event): void {
    const element = this._textarea().nativeElement;
    this._mention = mentionQueryAt(
      element.value,
      element.selectionStart,
      this.links().map((link) => link.label),
    );
    if (this._mention === null) {
      this._picker().hide();
      return;
    }
    this.mentionSearchRequested.emit(this._mention.query);
    this._picker().show(event, element);
  }

  protected onPick(option: MentionOption): void {
    const element = this._textarea().nativeElement;
    const mention = this._mention;
    this._picker().hide();
    if (mention === null || !canAddLink(this.links(), option)) {
      return;
    }
    const caret = element.selectionStart;
    const applied = applyMention(element.value, mention, Math.max(caret, mention.start + 1), option.label);
    this.model.set({ text: applied.text });
    this.links.update((links) => [...links, option]);
    this._mention = null;
    element.focus();
    element.setSelectionRange(applied.caret, applied.caret);
  }

  protected removeLink(option: MentionOption): void {
    this.links.update((links) => links.filter((link) => link !== option));
  }

  protected onSend(): void {
    if (this.disabledReason() !== null) {
      return;
    }
    const text = this.model().text.trim();
    this.sendRequested.emit({ text, links: linksForSend(text, this.links()) });
    this.model.set({ text: '' });
    this.links.set([]);
  }
}
