import { Component, effect, input, output, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Message } from 'primeng/message';
import { OverlayBadge } from 'primeng/overlaybadge';
import { Popover } from 'primeng/popover';
import { Tooltip } from 'primeng/tooltip';

import {
  hasJump,
  messageSeverityOf,
  type NotificationEntry,
  type NotificationItem,
} from '@shared/notifications/notification.store.utils';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

@Component({
  imports: [ButtonDirective, LocalDateTimePipe, Message, OverlayBadge, Popover, Tooltip, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-notification-bell',
  templateUrl: './notification-bell.html',
})
export class NotificationBell {
  readonly items = input.required<readonly NotificationItem[]>();
  readonly unreadCount = input.required<number>();
  readonly isOpen = input.required<boolean>();
  readonly opened = output();
  readonly closed = output();
  readonly jump = output<NotificationEntry>();
  readonly markAsRead = output<string>();
  readonly markAllAsRead = output();

  protected readonly popover = viewChild.required(Popover);
  protected readonly hasJump = hasJump;
  protected readonly messageSeverityOf = messageSeverityOf;

  constructor() {
    effect(() => {
      if (!this.isOpen()) {
        this.popover().hide();
      }
    });
  }
}
