import { Component, computed, effect, input, linkedSignal, output, untracked } from '@angular/core';
import { form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { Tooltip } from 'primeng/tooltip';

import { chatComposerSchema } from '@shared/chat/chat-composer/chat-composer-schema';
import { isCountShown, sendDisabledReason } from '@shared/chat/chat-composer/chat-composer.utils';
import { CHAT_TEXT_MAX_LENGTH } from '@shared/chat/chat-limits';

/** The box for a new message with Send and Stop. It reads no store. */
@Component({
  imports: [ButtonDirective, FormField, Textarea, Tooltip, TranslatePipe],
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

  readonly sendRequested = output<string>();
  readonly stopRequested = output<void>();

  protected readonly maxLength = CHAT_TEXT_MAX_LENGTH;
  protected readonly model = linkedSignal(() => ({ text: '' }));
  protected readonly composerForm = form(this.model, (path) => {
    validateStandardSchema(path, chatComposerSchema);
  });
  protected readonly disabledReason = computed(() =>
    sendDisabledReason({
      text: this.model().text,
      isRunning: this.isRunning(),
      isStopping: this.isStopping(),
    }),
  );
  protected readonly showCount = computed(() => isCountShown(this.model().text.length));

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

  protected onSend(): void {
    if (this.disabledReason() !== null) {
      return;
    }
    this.sendRequested.emit(this.model().text.trim());
    this.model.set({ text: '' });
  }
}
