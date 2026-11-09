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
import { MachinesApi } from '@machines/machines-api';

import { machineFilesResolver, NOT_FOUND_URL } from './machine-files.resolver';

const MACHINE_ID = 'encrypted-machine-id';
const MACHINE = { id: MACHINE_ID, name: 'Lathe 01' } as Machine;

const messages = { add: vi.fn() };

function resolve(get: (id: string) => Observable<Machine>): Promise<unknown[]> {
  TestBed.configureTestingModule({
    providers: [
      { provide: MachinesApi, useValue: { get } },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const route = { paramMap: convertToParamMap({ machineId: MACHINE_ID }) } as ActivatedRouteSnapshot;
  const result = TestBed.runInInjectionContext(() => machineFilesResolver(route, {} as RouterStateSnapshot));
  return firstValueFrom((result as Observable<unknown>).pipe(toArray()));
}

describe('machineFilesResolver', () => {
  beforeEach(() => {
    messages.add.mockClear();
  });

  it('machineFilesResolver_WithKnownMachine_ReturnsTheMachine', async () => {
    const get = vi.fn(() => of(MACHINE));

    const [result] = await resolve(get);

    expect(get).toHaveBeenCalledWith(MACHINE_ID, expect.anything());
    expect(result).toBe(MACHINE);
  });

  it('machineFilesResolver_WithNotFound_RedirectsToNotFoundPageKeepingTheUrl', async () => {
    const [result] = await resolve(() => throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));

    expect(messages.add).not.toHaveBeenCalled();
    expect(result).toBeInstanceOf(RedirectCommand);
    const redirect = result as RedirectCommand;
    expect(TestBed.inject(Router).serializeUrl(redirect.redirectTo as never)).toBe(NOT_FOUND_URL);
    expect(redirect.navigationBehaviorOptions).toEqual({ skipLocationChange: true });
  });

  it('machineFilesResolver_WithServerError_ShowsStatusToastAndCancelsNavigation', async () => {
    const values = await resolve(() =>
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.InternalServerError })),
    );

    expect(values).toEqual([]);
    expect(messages.add).toHaveBeenCalledTimes(1);
  });

  it('machineFilesResolver_WithNonHttpError_RethrowsIt', async () => {
    await expect(resolve(() => throwError(() => new Error('boom')))).rejects.toThrow('boom');
  });
});
