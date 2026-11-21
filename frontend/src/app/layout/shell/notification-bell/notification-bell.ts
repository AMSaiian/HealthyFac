import { Component, effect, input, output, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { OverlayBadge } from 'primeng/overlaybadge';
import { Popover } from 'primeng/popover';
import { Tooltip } from 'primeng/tooltip';

import { hasJump, type NotificationEntry, type NotificationItem } from '@shared/notifications/notification.store.utils';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

@Component({
  imports: [ButtonDirective, LocalDateTimePipe, OverlayBadge, Popover, Tooltip, TranslatePipe],
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

  protected readonly popover = viewChild.required(Popover);
  protected readonly hasJump = hasJump;

  constructor() {
    effect(() => {
      if (!this.isOpen()) {
        this.popover().hide();
      }
    });
  }
}
