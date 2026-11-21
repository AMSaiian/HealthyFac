import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Tooltip } from 'primeng/tooltip';

import { retryRequestedUrlAfterReload } from '@fallback/fallback-retry';
import { NOT_FOUND_URL } from '@shared/http/fallback-urls';
import { HistoryStore } from '@shared/history/history.store';
import { HOME_RETURN_TARGET } from '@shared/history/return-target';

@Component({
  imports: [ButtonDirective, Card, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 items-center justify-center p-4' },
  selector: 'hf-not-found-page',
  templateUrl: './not-found-page.html',
})
export class NotFoundPage {
  protected readonly history = inject(HistoryStore);
  protected readonly returnTarget = HOME_RETURN_TARGET;

  private readonly _router = inject(Router);

  constructor() {
    retryRequestedUrlAfterReload(NOT_FOUND_URL);
  }

  protected goHome(): void {
    void this._router.navigateByUrl('/');
  }
}
