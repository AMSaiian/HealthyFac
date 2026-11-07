import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  type ActivatedRouteSnapshot,
  convertToParamMap,
  RedirectCommand,
  Router,
  type RouterStateSnapshot,
} from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { firstValueFrom, type Observable, of, throwError, toArray } from 'rxjs';

import type { Machine } from '@machines/machine';
import { StatusDefinitionsGridStore } from '@machines/statuses/list/status-definitions-grid.store';

import { NOT_FOUND_URL, statusDefinitionsResolver } from './status-definitions.resolver';

const MACHINE_ID = 'encrypted-machine-id';
const MACHINE = { id: MACHINE_ID, name: 'Lathe 01' } as Machine;

function httpError(status: number): HttpErrorResponse {
  return new HttpErrorResponse({ status, statusText: String(status) });
}

const messages = { add: vi.fn() };

function resolve(openMachine: (id: string) => Observable<Machine>): Promise<unknown[]> {
  TestBed.configureTestingModule({
    providers: [
      { provide: StatusDefinitionsGridStore, useValue: { openMachine } },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const route = { paramMap: convertToParamMap({ machineId: MACHINE_ID }) } as ActivatedRouteSnapshot;
  const result = TestBed.runInInjectionContext(() => statusDefinitionsResolver(route, {} as RouterStateSnapshot));
  return firstValueFrom((result as Observable<unknown>).pipe(toArray()));
}

describe('statusDefinitionsResolver', () => {
  beforeEach(() => {
    messages.add.mockClear();
  });

  it('statusDefinitionsResolver_WithKnownMachine_OpensItAndReturnsTheMachine', async () => {
    const openMachine = vi.fn(() => of(MACHINE));

    const [result] = await resolve(openMachine);

    expect(openMachine).toHaveBeenCalledWith(MACHINE_ID);
    expect(result).toBe(MACHINE);
  });

  it('statusDefinitionsResolver_WithNotFound_RedirectsToNotFoundPageKeepingTheUrl', async () => {
    const [result] = await resolve(() => throwError(() => httpError(HttpStatusCode.NotFound)));

    expect(messages.add).not.toHaveBeenCalled();
    expect(result).toBeInstanceOf(RedirectCommand);
    const redirect = result as RedirectCommand;
    expect(TestBed.inject(Router).serializeUrl(redirect.redirectTo as never)).toBe(NOT_FOUND_URL);
    expect(redirect.navigationBehaviorOptions).toEqual({ skipLocationChange: true });
  });

  it('statusDefinitionsResolver_WithServerError_ShowsStatusToastAndCancelsNavigation', async () => {
    const values = await resolve(() => throwError(() => httpError(HttpStatusCode.InternalServerError)));

    expect(values).toEqual([]);
    expect(messages.add).toHaveBeenCalledExactlyOnceWith({
      severity: 'error',
      summary: 'ERRORS.SUMMARY',
      detail: 'ERRORS.HTTP.SERVER',
    });
  });
});
