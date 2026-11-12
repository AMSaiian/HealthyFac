import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { ActionStore } from '@shared/actions/action.store';
import { FileDownloader } from '@shared/files/file-downloader';
import { FilesLinksApi } from '@shared/files/files-links-api';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { FilePickerGridHost } from './file-picker-grid-host';

describe('FilePickerGridHost', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActionStore, useValue: { dispatch: vi.fn(), lastResult: () => null } },
        { provide: AuthStore, useValue: { hasPermission: () => false } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: FileDownloader, useValue: { start: vi.fn() } },
        { provide: FilesLinksApi, useValue: { getLinks: vi.fn() } },
        provideTranslateService(),
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
  });

  it('filePickerGridHost_WhenCreated_ProvidesTheIngestApiItsStoreNeeds', () => {
    const create = (): unknown => TestBed.createComponent(FilePickerGridHost);

    expect(create).not.toThrow();
  });
});
