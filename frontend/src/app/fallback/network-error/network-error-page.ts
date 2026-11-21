import { Location } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Tooltip } from 'primeng/tooltip';

import { retryRequestedUrlAfterReload } from '@fallback/fallback-retry';
import { NETWORK_ERROR_URL } from '@shared/http/fallback-urls';
import { HistoryStore } from '@shared/history/history.store';
import { HOME_RETURN_TARGET } from '@shared/history/return-target';

@Component({
  imports: [ButtonDirective, Card, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 items-center justify-center p-4' },
  selector: 'hf-network-error-page',
  templateUrl: './network-error-page.html',
})
export class NetworkErrorPage {
  protected readonly history = inject(HistoryStore);
  protected readonly returnTarget = HOME_RETURN_TARGET;

  private readonly _router = inject(Router);
  private readonly _location = inject(Location);

  constructor() {
    retryRequestedUrlAfterReload(NETWORK_ERROR_URL);
  }

  /**
   * Asks for the failed page again. The address bar shows the failed URL (`browserUrl`), so the router replaces the
   * entry and adds none.
   */
  protected retry(): void {
    void this._router.navigateByUrl(this._location.path());
  }
}
