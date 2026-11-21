import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';

import { HistoryStore } from '@shared/history/history.store';

import { NetworkErrorPage } from './network-error-page';

describe('NetworkErrorPage', () => {
  it('retry_NavigatesToTheBrowserUrl', () => {
    const navigateByUrl = vi.fn(() => Promise.resolve(true));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: Router, useValue: { navigateByUrl, currentNavigation: () => null } },
        { provide: Location, useValue: { path: () => '/maintenance/cases/c1' } },
        { provide: HistoryStore, useValue: { back: vi.fn() } },
      ],
    });
    const page = TestBed.createComponent(NetworkErrorPage).componentInstance as unknown as { retry(): void };

    page.retry();

    expect(navigateByUrl).toHaveBeenCalledExactlyOnceWith('/maintenance/cases/c1');
  });
});
